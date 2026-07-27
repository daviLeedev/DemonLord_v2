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
        [SerializeField] private LabDoorController archiveAnnexDoor;

        private IPlayerSession playerSession;
        private SaveGameProgressUseCase saveProgress;
        private int currentStage;
        private bool initialized;
        private ExplorationLocationState locationState;

        public void Configure(
            DialogueFocusController configuredDialogueController,
            NotificationView configuredNotificationView,
            PrototypeInteractable configuredResearcher,
            PrototypeInteractable configuredTaxLedger,
            PrototypeInteractable configuredArchiveCatalog,
            LabDoorController configuredArchiveAnnexDoor)
        {
            dialogueController = configuredDialogueController;
            notificationView = configuredNotificationView;
            researcher = configuredResearcher;
            taxLedger = configuredTaxLedger;
            archiveCatalog = configuredArchiveCatalog;
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
                archiveAnnexDoor = null;
                return;
            }

            areaRoot.TryGetInteractable("worldline-researcher", out researcher);
            areaRoot.TryGetInteractable("tax-ledger", out taxLedger);
            areaRoot.TryGetInteractable("archive-catalog", out archiveCatalog);
            areaRoot.TryGetDoor("door_archiveannex", out archiveAnnexDoor);
            if (currentStage >= 3)
            {
                archiveAnnexDoor?.Unlock();
            }
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
                TryAdvance(3, LabCheckpointId.ArchiveCatalogued, "분류 장부를 기록했습니다. 현재 구역의 조사가 완료되었습니다.");
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

            return string.Equals(checkpointId, LabCheckpointId.ArchiveCatalogued, StringComparison.Ordinal) ? 3 : -1;
        }
    }
}
