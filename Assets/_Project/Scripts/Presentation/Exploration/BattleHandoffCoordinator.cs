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

        private ExplorationLocationState locationState;
        private BattleLaunchRequest currentRequest;
        private IDisposable gateToken;
        private bool awaitingCompletedHandoff;

        public event Action<BattleLaunchRequest> BattleRequested;

        public BattleLaunchRequest CurrentRequest => currentRequest;

        public void Configure(
            DialogueFocusController configuredDialogueController,
            LabProgressController configuredProgressController,
            ExplorationInputReader configuredInputReader,
            BattlePreparationView configuredPreparationView,
            NotificationView configuredNotificationView)
        {
            dialogueController = configuredDialogueController;
            progressController = configuredProgressController;
            inputReader = configuredInputReader;
            preparationView = configuredPreparationView;
            notificationView = configuredNotificationView;
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
            if (progressController != null && progressController.IsCombatHandoffComplete)
            {
                ShowPreparation();
            }
        }

        private void OnObjectiveChanged(LabObjectiveState state)
        {
            if (awaitingCompletedHandoff && state.IsComplete) ShowPreparation();
        }

        private void ShowPreparation()
        {
            awaitingCompletedHandoff = false;
            ExplorationLocation returnLocation = locationState?.Current;
            if (returnLocation == null
                && !ExplorationLocation.TryCreate(
                    ExplorationAreaIds.WorldAdjustmentLabInterior,
                    ExplorationSpawnIds.ReceptionStart,
                    out returnLocation,
                    out _))
            {
                notificationView?.Show("전투 복귀 위치를 만들 수 없습니다.");
                return;
            }

            currentRequest = new BattleLaunchRequest(BattleId, EnemyGroupId, returnLocation);
            gateToken ??= inputReader?.Gate.AcquireLock(ExplorationInputChannel.All);
            inputReader?.ClearPendingMenuInput();
            preparationView?.Show(currentRequest);
        }

        private void OnDispatchRequested()
        {
            if (currentRequest == null) return;
            BattleRequested?.Invoke(currentRequest);
            preparationView?.SetStatus("전투 개발 모듈의 IBattleFlowService 연결을 기다리고 있습니다.");
            notificationView?.Show("출동 요청을 생성했습니다. 전투 모듈 연결 대기 중입니다.");
        }

        public void Close()
        {
            preparationView?.Hide();
            currentRequest = null;
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
