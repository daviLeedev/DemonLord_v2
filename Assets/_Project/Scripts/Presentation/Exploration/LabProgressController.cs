using System;
using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    /// <summary>
    /// Owns the short laboratory objective chain and maps completed objectives to
    /// durable save checkpoints. The restricted containment door deliberately is
    /// not part of this chain; it remains a locked future-content boundary.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LabProgressController : MonoBehaviour
    {
        [SerializeField] private DialogueFocusController dialogueController;
        [SerializeField] private NotificationView notificationView;
        [SerializeField] private PrototypeInteractable researcher;
        [SerializeField] private PrototypeInteractable taxLedger;
        [SerializeField] private PrototypeInteractable archiveCatalog;
        [SerializeField] private PrototypeInteractable combatLiaison;
        [SerializeField] private LabDoorController archiveAnnexDoor;

        private IPlayerSession playerSession;
        private SaveGameProgressUseCase saveProgress;
        private int currentStage;
        private bool initialized;
        private ExplorationLocationState locationState;

        public event Action<LabObjectiveState> ObjectiveChanged;

        public LabObjectiveState CurrentObjective => GetObjective(currentStage);

        public bool IsCombatHandoffComplete => currentStage >= 4;

        public void Configure(
            DialogueFocusController configuredDialogueController,
            NotificationView configuredNotificationView,
            PrototypeInteractable configuredResearcher,
            PrototypeInteractable configuredTaxLedger,
            PrototypeInteractable configuredArchiveCatalog,
            PrototypeInteractable configuredCombatLiaison,
            LabDoorController configuredArchiveAnnexDoor)
        {
            dialogueController = configuredDialogueController;
            notificationView = configuredNotificationView;
            researcher = configuredResearcher;
            taxLedger = configuredTaxLedger;
            archiveCatalog = configuredArchiveCatalog;
            combatLiaison = configuredCombatLiaison;
            archiveAnnexDoor = configuredArchiveAnnexDoor;
        }

        public bool TryInitialize(IPlayerSession configuredSession, SaveGameProgressUseCase configuredSaveProgress, out string errorCode)
        {
            errorCode = null;
            if (initialized)
            {
                return true;
            }

            if (configuredSession == null || configuredSession.CurrentSave == null)
            {
                errorCode = "progress_active_save_missing";
                return false;
            }

            playerSession = configuredSession;
            saveProgress = configuredSaveProgress;
            currentStage = GetStage(configuredSession.CurrentSave.Progress.CheckpointId);
            if (currentStage < 0)
            {
                errorCode = "progress_checkpoint_unknown";
                return false;
            }

            if (currentStage >= 3)
            {
                archiveAnnexDoor?.Unlock();
            }

            // The standalone GameShell tests intentionally construct no persistence
            // service. In an actual game launch this is always supplied by AppRoot.
            if (saveProgress != null && dialogueController != null)
            {
                dialogueController.DialogueCompleted += OnDialogueCompleted;
            }

            initialized = true;
            ApplyObjectiveMarkers();
            ObjectiveChanged?.Invoke(CurrentObjective);
            return true;
        }

        public void BindAreaContent(AreaRoot areaRoot, ExplorationLocationState configuredLocationState)
        {
            locationState = configuredLocationState;
            if (areaRoot == null
                || areaRoot.Definition == null
                || !string.Equals(
                    areaRoot.Definition.AreaId,
                    ExplorationAreaIds.WorldAdjustmentLabInterior,
                    StringComparison.Ordinal))
            {
                researcher = null;
                taxLedger = null;
                archiveCatalog = null;
                combatLiaison = null;
                archiveAnnexDoor = null;
                return;
            }

            areaRoot.TryGetInteractable("worldline-researcher", out researcher);
            areaRoot.TryGetInteractable("tax-ledger", out taxLedger);
            areaRoot.TryGetInteractable("archive-catalog", out archiveCatalog);
            areaRoot.TryGetInteractable("combat-liaison-officer", out combatLiaison);
            areaRoot.TryGetDoor("door_archiveannex", out archiveAnnexDoor);
            if (currentStage >= 3)
            {
                archiveAnnexDoor?.Unlock();
            }

            ApplyObjectiveMarkers();
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        private void OnDisable()
        {
            if (dialogueController != null)
            {
                dialogueController.DialogueCompleted -= OnDialogueCompleted;
            }

            initialized = false;
        }

        private void OnDialogueCompleted(PrototypeInteractable interactable)
        {
            if (interactable == researcher)
            {
                TryAdvance(1, LabCheckpointId.ResearcherBriefed, "연구원 보고를 기록했습니다. 세무 기록부를 조사하십시오.");
                return;
            }

            if (interactable == taxLedger)
            {
                TryAdvance(2, LabCheckpointId.TaxLedgerReviewed, "세무 기록부를 기록했습니다. 기록보관실의 분류 장부를 조사하십시오.");
                return;
            }

            if (interactable == archiveCatalog)
            {
                TryAdvance(3, LabCheckpointId.ArchiveCatalogued, "분류 장부를 기록했습니다. 전투 대응 집행관에게 조사 결과를 인계하십시오.");
                return;
            }

            if (interactable == combatLiaison)
            {
                TryAdvance(4, LabCheckpointId.CombatLiaisonBriefed, "현장 인계를 완료했습니다. 전투 시퀀스 연결 준비가 끝났습니다.");
            }
        }

        private void TryAdvance(int targetStage, string checkpointId, string successMessage)
        {
            if (!initialized || saveProgress == null || targetStage <= currentStage)
            {
                return;
            }

            if (targetStage != currentStage + 1)
            {
                notificationView?.Show("먼저 이전 조사 항목을 확인해야 합니다.");
                return;
            }

            SaveWriteResult result = locationState != null
                ? saveProgress.Execute(playerSession, checkpointId, locationState.Current)
                : saveProgress.Execute(playerSession, checkpointId);
            if (!result.IsSuccess)
            {
                Debug.LogWarning("Unable to save laboratory progress: " + result.ErrorCode, this);
                notificationView?.Show("기록 저장에 실패했습니다. 다시 시도하십시오.");
                return;
            }

            currentStage = targetStage;
            if (targetStage >= 3)
            {
                archiveAnnexDoor?.Unlock();
            }

            notificationView?.Show(successMessage);
            ApplyObjectiveMarkers();
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        private void ApplyObjectiveMarkers()
        {
            string targetId = CurrentObjective.TargetStableId;
            SetObjectiveMarker(researcher, targetId);
            SetObjectiveMarker(taxLedger, targetId);
            SetObjectiveMarker(archiveCatalog, targetId);
            SetObjectiveMarker(combatLiaison, targetId);
        }

        private static void SetObjectiveMarker(PrototypeInteractable interactable, string targetId)
        {
            if (interactable == null) return;
            WorldInteractionIndicator indicator = interactable.GetComponentInChildren<WorldInteractionIndicator>(true);
            indicator?.SetObjectiveTarget(string.Equals(interactable.StableId, targetId, StringComparison.Ordinal));
        }

        private static LabObjectiveState GetObjective(int stage)
        {
            switch (stage)
            {
                case 0:
                    return new LabObjectiveState(0, "세계선 분석 연구원에게 보고받기", "worldline-researcher", false);
                case 1:
                    return new LabObjectiveState(1, "세무 기록부 조사", "tax-ledger", false);
                case 2:
                    return new LabObjectiveState(2, "기록보관실 분류 장부 조사", "archive-catalog", false);
                case 3:
                    return new LabObjectiveState(3, "전투 대응 집행관에게 조사 결과 인계", "combat-liaison-officer", false);
                default:
                    return new LabObjectiveState(4, "현재 업무 완료", string.Empty, true);
            }
        }

        private static int GetStage(string checkpointId)
        {
            if (string.Equals(checkpointId, LabCheckpointId.Start, StringComparison.Ordinal))
            {
                return 0;
            }

            if (string.Equals(checkpointId, LabCheckpointId.ResearcherBriefed, StringComparison.Ordinal))
            {
                return 1;
            }

            if (string.Equals(checkpointId, LabCheckpointId.TaxLedgerReviewed, StringComparison.Ordinal))
            {
                return 2;
            }

            if (string.Equals(checkpointId, LabCheckpointId.ArchiveCatalogued, StringComparison.Ordinal))
            {
                return 3;
            }

            return string.Equals(checkpointId, LabCheckpointId.CombatLiaisonBriefed, StringComparison.Ordinal) ? 4 : -1;
        }
    }
}
