using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DemonLord.Application;
using DemonLord.Bootstrap;
using DemonLord.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class GameShellRoot : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private CharacterController playerController;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private ExplorationInputReader inputReader;
        [SerializeField] private PlayerFacing playerFacing;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private QuarterViewCameraRig cameraRig;
        [SerializeField] private InteractionSensor interactionSensor;
        [SerializeField] private GameShellSessionView sessionView;
        [SerializeField] private LabProgressController progressController;
        [SerializeField] private InGameHudView inGameHudView;
        [SerializeField] private LocationTracker locationTracker;
        [SerializeField] private InGameUiCoordinator inGameUiCoordinator;
        [SerializeField] private AreaTransitionCoordinator areaTransitionCoordinator;
        [SerializeField] private MapCoordinator mapCoordinator;
        [SerializeField] private BattleHandoffCoordinator battleHandoffCoordinator;
        [SerializeField] private GameObject[] legacyAreaContentRoots = Array.Empty<GameObject>();
        [SerializeField] private SpawnPoint[] spawnPoints = Array.Empty<SpawnPoint>();

        private bool initialized;
        private InGameHudStateSource hudStateSource;

        public bool IsInitialized => initialized;

        public Transform PlayerRoot => playerRoot;

        public Camera GameCamera => gameCamera;

        public PlayerMotor PlayerMotor => playerMotor;

        public ExplorationInputReader InputReader => inputReader;

        public QuarterViewCameraRig CameraRig => cameraRig;

        public InteractionSensor InteractionSensor => interactionSensor;

        public InGameHudView InGameHudView => inGameHudView;

        public LocationTracker LocationTracker => locationTracker;

        public InGameUiCoordinator InGameUiCoordinator => inGameUiCoordinator;

        public BattleHandoffCoordinator BattleHandoffCoordinator => battleHandoffCoordinator;

        public AreaTransitionCoordinator AreaTransitionCoordinator => areaTransitionCoordinator;

        public MapCoordinator MapCoordinator => mapCoordinator;

        private void Awake()
        {
            SetExplorationEnabled(false);
        }

        public void Configure(
            Transform configuredPlayerRoot,
            CharacterController configuredPlayerController,
            Camera configuredGameCamera,
            ExplorationInputReader configuredInputReader,
            PlayerFacing configuredPlayerFacing,
            PlayerMotor configuredPlayerMotor,
            QuarterViewCameraRig configuredCameraRig,
            InteractionSensor configuredInteractionSensor,
            GameShellSessionView configuredSessionView,
            SpawnPoint[] configuredSpawnPoints,
            LabProgressController configuredProgressController = null,
            InGameHudView configuredInGameHudView = null,
            LocationTracker configuredLocationTracker = null,
            InGameUiCoordinator configuredInGameUiCoordinator = null,
            AreaTransitionCoordinator configuredAreaTransitionCoordinator = null,
            MapCoordinator configuredMapCoordinator = null,
            BattleHandoffCoordinator configuredBattleHandoffCoordinator = null)
        {
            playerRoot = configuredPlayerRoot;
            playerController = configuredPlayerController;
            gameCamera = configuredGameCamera;
            inputReader = configuredInputReader;
            playerFacing = configuredPlayerFacing;
            playerMotor = configuredPlayerMotor;
            cameraRig = configuredCameraRig;
            interactionSensor = configuredInteractionSensor;
            sessionView = configuredSessionView;
            progressController = configuredProgressController;
            inGameHudView = configuredInGameHudView;
            locationTracker = configuredLocationTracker;
            inGameUiCoordinator = configuredInGameUiCoordinator;
            areaTransitionCoordinator = configuredAreaTransitionCoordinator;
            mapCoordinator = configuredMapCoordinator;
            battleHandoffCoordinator = configuredBattleHandoffCoordinator;
            spawnPoints = configuredSpawnPoints == null
                ? Array.Empty<SpawnPoint>()
                : (SpawnPoint[])configuredSpawnPoints.Clone();
        }

        public bool TryInitialize(
            IPlayerSession playerSession,
            EntryDestination destination,
            out string errorCode)
        {
            return TryInitialize(playerSession, destination, null, null, null, null, out errorCode);
        }

        public bool TryInitialize(
            IPlayerSession playerSession,
            EntryDestination destination,
            SaveGameProgressUseCase saveProgress,
            out string errorCode)
        {
            return TryInitialize(playerSession, destination, saveProgress, null, null, null, out errorCode);
        }

        public bool TryInitialize(
            IPlayerSession playerSession,
            EntryDestination destination,
            SaveGameProgressUseCase saveProgress,
            SettingsService settingsService,
            ISceneFlowService sceneFlowService,
            IApplicationQuitter applicationQuitter,
            out string errorCode)
        {
            errorCode = null;

            if (initialized)
            {
                errorCode = "game_shell_already_initialized";
                return false;
            }

            if (playerSession == null)
            {
                errorCode = "player_session_missing";
                return false;
            }

            if (playerSession.CurrentSave == null)
            {
                errorCode = "active_save_missing";
                return false;
            }

            if (destination == null)
            {
                errorCode = "entry_destination_missing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(destination.SceneKey)
                || string.IsNullOrWhiteSpace(destination.SpawnKey))
            {
                errorCode = "entry_destination_invalid";
                return false;
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid()
                || !string.Equals(scene.name, destination.SceneKey, StringComparison.Ordinal))
            {
                errorCode = "entry_scene_mismatch";
                return false;
            }

            if (playerRoot == null
                || playerController == null
                || gameCamera == null
                || inputReader == null
                || playerFacing == null
                || playerMotor == null
                || cameraRig == null
                || interactionSensor == null)
            {
                errorCode = "game_shell_reference_missing";
                return false;
            }

            if (!TryFindSpawnPoint(destination.SpawnKey, out SpawnPoint spawnPoint, out errorCode))
            {
                return false;
            }

            bool controllerWasEnabled = playerController.enabled;
            playerController.enabled = false;
            playerRoot.SetPositionAndRotation(spawnPoint.Pose.position, spawnPoint.Pose.rotation);
            playerController.enabled = controllerWasEnabled;
            Physics.SyncTransforms();

            cameraRig.Configure(gameCamera, playerRoot, inputReader, inputReader.Gate);
            playerMotor.Initialize(inputReader, cameraRig, playerFacing);
            playerMotor.ResetMotion();
            cameraRig.SnapImmediate();
            if (progressController != null
                && !progressController.TryInitialize(playerSession, saveProgress, out errorCode))
            {
                return false;
            }

            bool initializeInGameUi = settingsService != null || sceneFlowService != null || applicationQuitter != null;
            if (initializeInGameUi)
            {
                if (inGameHudView == null || locationTracker == null || inGameUiCoordinator == null)
                {
                    errorCode = "in_game_hud_reference_missing";
                    return false;
                }

                if (settingsService == null || sceneFlowService == null || applicationQuitter == null || saveProgress == null)
                {
                    errorCode = "in_game_hud_service_missing";
                    return false;
                }

                hudStateSource = new InGameHudStateSource(
                    "세계조정국 연구실",
                    "중앙 접수실",
                    new EmptyWalletReadModel(),
                    new EmptyGameTimeReadModel());
                locationTracker.Configure(playerRoot);
                locationTracker.Initialize(hudStateSource);
                inGameHudView.Initialize(hudStateSource);
                inGameHudView.BindObjectiveSource(progressController);
                if (!inGameUiCoordinator.TryInitialize(
                        playerSession,
                        saveProgress,
                        settingsService,
                        sceneFlowService,
                        applicationQuitter,
                        out errorCode))
                {
                    hudStateSource.Dispose();
                    hudStateSource = null;
                    return false;
                }
            }

            sessionView?.SetSession(playerSession);
            initialized = true;
            SetExplorationEnabled(true);
            return true;
        }

        public async Task<string> InitializeAsync(
            IPlayerSession playerSession,
            EntryDestination destination,
            SaveGameProgressUseCase saveProgress,
            SettingsService settingsService,
            ISceneFlowService sceneFlowService,
            IApplicationQuitter applicationQuitter)
        {
            if (areaTransitionCoordinator == null)
            {
                return TryInitialize(
                    playerSession,
                    destination,
                    saveProgress,
                    settingsService,
                    sceneFlowService,
                    applicationQuitter,
                    out string legacyError)
                    ? null
                    : legacyError;
            }

            string errorCode = ValidateAreaInitialization(
                playerSession,
                destination,
                saveProgress,
                settingsService,
                sceneFlowService,
                applicationQuitter);
            if (errorCode != null)
            {
                return errorCode;
            }

            cameraRig.Configure(gameCamera, playerRoot, inputReader, inputReader.Gate);
            playerMotor.Initialize(inputReader, cameraRig, playerFacing);
            playerMotor.ResetMotion();

            if (progressController != null
                && !progressController.TryInitialize(playerSession, saveProgress, out errorCode))
            {
                return errorCode;
            }

            hudStateSource = new InGameHudStateSource(
                string.Empty,
                string.Empty,
                new EmptyWalletReadModel(),
                new EmptyGameTimeReadModel());
            locationTracker.Configure(playerRoot);
            locationTracker.Initialize(hudStateSource);
            inGameHudView.Initialize(hudStateSource);
            inGameHudView.BindObjectiveSource(progressController);

            SetLegacyAreaContentEnabled(false);

            if (!ExplorationLocation.TryCreate(
                    destination.AreaId,
                    destination.SpawnKey,
                    out ExplorationLocation initialLocation,
                    out errorCode))
            {
                DisposeHudState();
                SetLegacyAreaContentEnabled(true);
                return errorCode;
            }

            AreaTransitionResult transitionResult = await areaTransitionCoordinator.InitializeAsync(initialLocation);
            if (!transitionResult.IsSuccess)
            {
                DisposeHudState();
                SetLegacyAreaContentEnabled(true);
                return transitionResult.ErrorCode;
            }

            progressController?.BindAreaContent(
                areaTransitionCoordinator.CurrentAreaRoot,
                areaTransitionCoordinator.LocationState);
            battleHandoffCoordinator?.BindLocationState(areaTransitionCoordinator.LocationState);
            areaTransitionCoordinator.AreaChanged += OnAreaChanged;

            if (!inGameUiCoordinator.TryInitialize(
                    playerSession,
                    saveProgress,
                    settingsService,
                    sceneFlowService,
                    applicationQuitter,
                    out errorCode))
            {
                DisposeHudState();
                SetLegacyAreaContentEnabled(true);
                return errorCode;
            }

            sessionView?.SetSession(playerSession);
            initialized = true;
            SetExplorationEnabled(true);
            return null;
        }

        private string ValidateAreaInitialization(
            IPlayerSession playerSession,
            EntryDestination destination,
            SaveGameProgressUseCase saveProgress,
            SettingsService settingsService,
            ISceneFlowService sceneFlowService,
            IApplicationQuitter applicationQuitter)
        {
            if (initialized)
            {
                return "game_shell_already_initialized";
            }

            if (playerSession == null)
            {
                return "player_session_missing";
            }

            if (playerSession.CurrentSave == null)
            {
                return "active_save_missing";
            }

            if (destination == null)
            {
                return "entry_destination_missing";
            }

            if (string.IsNullOrWhiteSpace(destination.SceneKey)
                || string.IsNullOrWhiteSpace(destination.AreaId)
                || string.IsNullOrWhiteSpace(destination.SpawnKey))
            {
                return "entry_destination_invalid";
            }

            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !string.Equals(scene.name, destination.SceneKey, StringComparison.Ordinal))
            {
                return "entry_scene_mismatch";
            }

            if (playerRoot == null || playerController == null || gameCamera == null
                || inputReader == null || playerFacing == null || playerMotor == null
                || cameraRig == null || interactionSensor == null || inGameHudView == null
                || locationTracker == null || inGameUiCoordinator == null || mapCoordinator == null)
            {
                return "game_shell_reference_missing";
            }

            if (saveProgress == null || settingsService == null || sceneFlowService == null || applicationQuitter == null)
            {
                return "game_shell_service_missing";
            }

            return null;
        }

        private void OnDestroy()
        {
            if (areaTransitionCoordinator != null)
            {
                areaTransitionCoordinator.AreaChanged -= OnAreaChanged;
            }

            DisposeHudState();
        }

        private void OnAreaChanged(AreaDefinition definition, AreaRoot areaRoot)
        {
            progressController?.BindAreaContent(areaRoot, areaTransitionCoordinator.LocationState);
            battleHandoffCoordinator?.BindLocationState(areaTransitionCoordinator.LocationState);
        }

        private void DisposeHudState()
        {
            hudStateSource?.Dispose();
            hudStateSource = null;
        }

        private void SetLegacyAreaContentEnabled(bool enabled)
        {
            foreach (GameObject root in legacyAreaContentRoots ?? Array.Empty<GameObject>())
            {
                if (root != null && root.activeSelf != enabled)
                {
                    root.SetActive(enabled);
                }
            }
        }

        private void SetExplorationEnabled(bool enabled)
        {
            if (inputReader != null)
            {
                inputReader.enabled = enabled;
            }

            if (playerMotor != null)
            {
                playerMotor.enabled = enabled;
            }

            if (interactionSensor != null)
            {
                interactionSensor.enabled = enabled;
            }
        }

        private bool TryFindSpawnPoint(
            string requestedKey,
            out SpawnPoint match,
            out string errorCode)
        {
            match = null;
            errorCode = null;
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                errorCode = "spawn_points_missing";
                return false;
            }

            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint == null || string.IsNullOrWhiteSpace(spawnPoint.SpawnKey))
                {
                    errorCode = "spawn_point_invalid";
                    return false;
                }

                if (!keys.Add(spawnPoint.SpawnKey))
                {
                    errorCode = "spawn_point_duplicate";
                    return false;
                }

                if (string.Equals(spawnPoint.SpawnKey, requestedKey, StringComparison.Ordinal))
                {
                    match = spawnPoint;
                }
            }

            if (match == null)
            {
                errorCode = "spawn_point_not_found";
                return false;
            }

            return true;
        }
    }
}
