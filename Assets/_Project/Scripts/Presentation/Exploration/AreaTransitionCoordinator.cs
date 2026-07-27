using System;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AreaTransitionCoordinator : MonoBehaviour
    {
        private const float FadeOutSeconds = 0.25f;
        private const float FadeInSeconds = 0.35f;

        [SerializeField] private AreaRegistry registry;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private CharacterController playerController;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private QuarterViewCameraRig cameraRig;
        [SerializeField] private ExplorationInputReader inputReader;
        [SerializeField] private LocationTracker locationTracker;
        [SerializeField] private DialogueFocusController dialogueController;
        [SerializeField] private NotificationView notificationView;
        [SerializeField] private ScreenFadeView fadeView;

        private readonly AreaTransitionStateMachine stateMachine = new AreaTransitionStateMachine();
        private IAreaSceneLoader sceneLoader;
        private ExplorationLocationState locationState;
        private AreaRoot currentAreaRoot;
        private IDisposable gateToken;
        private bool disposed;

        public event Action<AreaDefinition, AreaRoot> AreaChanged;
        public bool IsBusy => stateMachine.IsBusy;
        public AreaRoot CurrentAreaRoot => currentAreaRoot;
        public ExplorationLocationState LocationState => locationState;

        public void Configure(
            AreaRegistry configuredRegistry,
            Transform configuredPlayerRoot,
            CharacterController configuredPlayerController,
            PlayerMotor configuredPlayerMotor,
            QuarterViewCameraRig configuredCameraRig,
            ExplorationInputReader configuredInputReader,
            LocationTracker configuredLocationTracker,
            DialogueFocusController configuredDialogueController,
            NotificationView configuredNotificationView,
            ScreenFadeView configuredFadeView)
        {
            registry = configuredRegistry;
            playerRoot = configuredPlayerRoot;
            playerController = configuredPlayerController;
            playerMotor = configuredPlayerMotor;
            cameraRig = configuredCameraRig;
            inputReader = configuredInputReader;
            locationTracker = configuredLocationTracker;
            dialogueController = configuredDialogueController;
            notificationView = configuredNotificationView;
            fadeView = configuredFadeView;
        }

        public void SetSceneLoaderForTests(IAreaSceneLoader configuredSceneLoader)
        {
            sceneLoader = configuredSceneLoader;
        }

        public async Task<AreaTransitionResult> InitializeAsync(ExplorationLocation initialLocation)
        {
            if (locationState != null)
            {
                return AreaTransitionResult.Failure("area_transition_already_initialized");
            }

            locationState = new ExplorationLocationState(initialLocation, string.Empty);
            return await TransitionAsync(new AreaTransitionRequest(initialLocation), true);
        }

        public bool RequestTransition(string targetAreaId, string targetSpawnId)
        {
            if (disposed || stateMachine.IsBusy
                || !ExplorationLocation.TryCreate(targetAreaId, targetSpawnId, out ExplorationLocation destination, out _))
            {
                return false;
            }

            _ = TransitionAsync(new AreaTransitionRequest(destination), false);
            return true;
        }

        public async Task<AreaTransitionResult> TransitionAsync(AreaTransitionRequest request, bool initialLoad = false)
        {
            if (request == null || disposed || !stateMachine.TryBegin())
            {
                return AreaTransitionResult.Failure("area_transition_rejected");
            }

            if (registry == null)
            {
                stateMachine.ForceIdle();
                return AreaTransitionResult.Failure("area_registry_missing");
            }

            if (!registry.TryValidate(out string registryError))
            {
                stateMachine.ForceIdle();
                return AreaTransitionResult.Failure(registryError);
            }

            if (!registry.TryGet(request.Destination.AreaId.Value, out AreaDefinition definition))
            {
                stateMachine.ForceIdle();
                return AreaTransitionResult.Failure("area_definition_not_found");
            }

            sceneLoader ??= new UnityAreaSceneLoader();
            Pose originalPose = playerRoot != null ? new Pose(playerRoot.position, playerRoot.rotation) : default;
            AreaRoot candidate = null;
            try
            {
                AcquireGate();
                if (fadeView != null)
                {
                    await fadeView.FadeToAsync(1f, initialLoad ? 0.01f : FadeOutSeconds);
                }

                RequireAdvance(AreaTransitionState.FadingOut, AreaTransitionState.Loading);
                candidate = await sceneLoader.LoadAsync(definition);
                RequireAdvance(AreaTransitionState.Loading, AreaTransitionState.Validating);
                if (candidate == null)
                {
                    throw new InvalidOperationException("area_root_invalid");
                }

                if (!candidate.TryValidate(definition.AreaId, out string validationError))
                {
                    throw new InvalidOperationException(validationError);
                }

                if (!candidate.TryGetSpawn(request.Destination.SpawnId.Value, out AreaSpawnPoint spawn))
                {
                    throw new InvalidOperationException("area_spawn_not_found");
                }

                RequireAdvance(AreaTransitionState.Validating, AreaTransitionState.Positioning);
                candidate.BindRuntime(this, locationTracker, locationState, playerRoot, cameraRig, dialogueController, notificationView);
                PlacePlayer(spawn.Pose);
                RequireAdvance(AreaTransitionState.Positioning, AreaTransitionState.UnloadingPrevious);
                AreaRoot previous = currentAreaRoot;
                if (previous != null)
                {
                    await sceneLoader.UnloadAsync(previous);
                }

                // Do not publish the new area/location until the old scene was released.
                // If unloading fails, candidate is still owned by this method and catch rolls
                // it back while currentAreaRoot and the last saveable location stay intact.
                currentAreaRoot = candidate;
                candidate = null;
                locationState.Set(request.Destination, spawn.FloorId);
                locationTracker?.BeginArea(
                    definition.AreaId,
                    definition.FallbackDisplayName,
                    string.Empty,
                    string.Empty,
                    spawn.FloorId);

                RequireAdvance(AreaTransitionState.UnloadingPrevious, AreaTransitionState.FadingIn);
                AreaChanged?.Invoke(definition, currentAreaRoot);
                if (fadeView != null)
                {
                    await fadeView.FadeToAsync(0f, FadeInSeconds);
                }

                stateMachine.TryComplete();
                ReleaseGate();
                return AreaTransitionResult.Success();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                stateMachine.TryBeginRollback();
                if (candidate != null)
                {
                    try
                    {
                        await sceneLoader.UnloadAsync(candidate);
                    }
                    catch (Exception unloadException)
                    {
                        Debug.LogException(unloadException, this);
                    }
                }

                if (playerRoot != null)
                {
                    PlacePlayer(originalPose);
                }

                fadeView?.SetImmediate(0f);
                stateMachine.TryComplete();
                ReleaseGate();
                notificationView?.Show("지역을 이동하지 못했습니다. 다시 시도해 주세요.");
                return AreaTransitionResult.Failure(exception.Message);
            }
        }

        private void OnDestroy()
        {
            disposed = true;
            ReleaseGate();
            fadeView?.SetImmediate(0f);
        }

        private void PlacePlayer(Pose pose)
        {
            if (playerRoot == null)
            {
                throw new InvalidOperationException("area_player_root_missing");
            }

            bool controllerWasEnabled = playerController != null && playerController.enabled;
            if (playerController != null) playerController.enabled = false;
            playerRoot.SetPositionAndRotation(pose.position, pose.rotation);
            if (playerController != null) playerController.enabled = controllerWasEnabled;
            Physics.SyncTransforms();
            playerMotor?.ResetMotion();
            cameraRig?.SnapImmediate();
        }

        private void AcquireGate()
        {
            if (gateToken == null && inputReader != null)
            {
                gateToken = inputReader.Gate.AcquireLock(ExplorationInputChannel.All);
                inputReader.ClearPendingMenuInput();
            }
        }

        private void ReleaseGate()
        {
            IDisposable token = gateToken;
            gateToken = null;
            token?.Dispose();
        }

        private void RequireAdvance(AreaTransitionState current, AreaTransitionState next)
        {
            if (!stateMachine.TryAdvance(current, next))
            {
                throw new InvalidOperationException("Invalid area transition state: " + stateMachine.State);
            }
        }
    }
}
