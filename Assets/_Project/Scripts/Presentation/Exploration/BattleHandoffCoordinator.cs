using System;
using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class BattleHandoffCoordinator : MonoBehaviour
    {
        private const string BattleId = "lab-first-contact";
        private const string EnemyGroupId = "adjustment-anomaly-alpha";

        [SerializeField] private DialogueFocusController dialogueController;
        [SerializeField] private LabProgressController progressController;
        [SerializeField] private ExplorationInputReader inputReader;
        [SerializeField] private BattlePreparationView preparationView;
        [SerializeField] private NotificationView notificationView;
        [SerializeField] private MonoBehaviour battleFlowServiceSource;

        private ExplorationLocationState locationState;
        private BattleLaunchRequest currentRequest;
        private IDisposable gateToken;
        private bool awaitingCompletedHandoff;
        private bool dispatchInProgress;

        public event Action<BattleLaunchRequest> BattleRequested;

        public BattleLaunchRequest CurrentRequest => currentRequest;

        public void Configure(
            DialogueFocusController configuredDialogueController,
            LabProgressController configuredProgressController,
            ExplorationInputReader configuredInputReader,
            BattlePreparationView configuredPreparationView,
            NotificationView configuredNotificationView,
            MonoBehaviour configuredBattleFlowServiceSource = null)
        {
            dialogueController = configuredDialogueController;
            progressController = configuredProgressController;
            inputReader = configuredInputReader;
            preparationView = configuredPreparationView;
            notificationView = configuredNotificationView;
            battleFlowServiceSource = configuredBattleFlowServiceSource;
        }

        public void BindLocationState(ExplorationLocationState configuredLocationState)
        {
            locationState = configuredLocationState;
        }

        private void OnEnable()
        {
            if (dialogueController != null) dialogueController.DialogueCompleted += OnDialogueCompleted;
            if (progressController != null) progressController.ObjectiveChanged += OnObjectiveChanged;
            if (preparationView != null)
            {
                preparationView.DispatchRequested += OnDispatchRequested;
                preparationView.CloseRequested += Close;
            }
        }

        private void OnDisable()
        {
            if (dialogueController != null) dialogueController.DialogueCompleted -= OnDialogueCompleted;
            if (progressController != null) progressController.ObjectiveChanged -= OnObjectiveChanged;
            if (preparationView != null)
            {
                preparationView.DispatchRequested -= OnDispatchRequested;
                preparationView.CloseRequested -= Close;
                preparationView.Hide();
            }

            ReleaseGate();
        }

        private void OnDialogueCompleted(PrototypeInteractable interactable)
        {
            if (interactable == null
                || !string.Equals(interactable.StableId, "combat-liaison-officer", StringComparison.Ordinal))
            {
                return;
            }

            awaitingCompletedHandoff = true;
            ShowPreparation();
        }

        private void OnObjectiveChanged(LabObjectiveState state)
        {
            if (awaitingCompletedHandoff && state.IsComplete) ShowPreparation();
        }

        private void ShowPreparation()
        {
            if (dispatchInProgress) return;

            awaitingCompletedHandoff = false;
            if (!TryCreateLaunchRequest(out BattleLaunchRequest request)) return;

            currentRequest = request;
            gateToken ??= inputReader?.Gate.AcquireLock(ExplorationInputChannel.All);
            inputReader?.ClearPendingMenuInput();
            preparationView?.Show(request);
        }

        private bool TryCreateLaunchRequest(out BattleLaunchRequest request)
        {
            ExplorationLocation returnLocation = locationState?.Current;
            if (returnLocation == null
                && !ExplorationLocation.TryCreate(
                    ExplorationAreaIds.WorldAdjustmentLabInterior,
                    ExplorationSpawnIds.ReceptionStart,
                    out returnLocation,
                    out _))
            {
                notificationView?.Show("전투 복귀 위치를 만들 수 없습니다.");
                request = null;
                return false;
            }

            request = new BattleLaunchRequest(BattleId, EnemyGroupId, returnLocation);
            return true;
        }

        private void OnDispatchRequested()
        {
            if (currentRequest == null || dispatchInProgress) return;
            BeginLaunch(currentRequest);
        }

        private async void BeginLaunch(BattleLaunchRequest request)
        {
            if (request == null || dispatchInProgress) return;

            BattleRequested?.Invoke(request);
            if (!(battleFlowServiceSource is IBattleFlowService battleFlowService))
            {
                preparationView?.SetStatus("전투 모듈이 아직 연결되지 않았습니다.");
                notificationView?.Show("전투 모듈 연결을 확인해 주세요.");
                return;
            }

            dispatchInProgress = true;
            preparationView?.SetStatus("모의전투를 준비하고 있습니다...");
            try
            {
                BattleLaunchResult result = await battleFlowService.LaunchAsync(request);
                if (!result.IsSuccess)
                {
                    preparationView?.SetStatus("출동 실패: " + result.ErrorCode);
                    notificationView?.Show("모의전투를 시작하지 못했습니다.");
                    return;
                }

                Close();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                preparationView?.SetStatus("전투 모듈 실행 중 오류가 발생했습니다.");
                notificationView?.Show("모의전투 실행 오류를 확인해 주세요.");
            }
            finally
            {
                dispatchInProgress = false;
            }
        }

        public void Close()
        {
            preparationView?.Hide();
            currentRequest = null;
            dispatchInProgress = false;
            ReleaseGate();
        }

        private void ReleaseGate()
        {
            IDisposable token = gateToken;
            gateToken = null;
            token?.Dispose();
        }
    }
}
