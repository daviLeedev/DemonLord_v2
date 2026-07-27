using System;

namespace DemonLord.Domain
{
    public enum SaveSlotState
    {
        Empty,
        Valid,
        Corrupt,
        Incompatible,
    }

    public sealed class SaveSlotSummary
    {
        public SaveSlotSummary(
            SaveSlotId slotId,
            SaveSlotState state,
            string profileName,
            string difficultyId,
            DateTime? updatedAtUtc,
            bool recoveredFromBackup,
            string errorCode,
            long playTimeSeconds = 0,
            string entryId = null,
            string checkpointId = null)
        {
            SlotId = slotId ?? throw new ArgumentNullException(nameof(slotId));
            State = state;
            ProfileName = profileName;
            DifficultyId = difficultyId;
            UpdatedAtUtc = updatedAtUtc;
            RecoveredFromBackup = recoveredFromBackup;
            ErrorCode = errorCode;
            PlayTimeSeconds = playTimeSeconds < 0 ? 0 : playTimeSeconds;
            EntryId = entryId;
            CheckpointId = checkpointId;
        }

        public SaveSlotId SlotId { get; }

        public SaveSlotState State { get; }

        public string ProfileName { get; }

        public string DifficultyId { get; }

        public DateTime? UpdatedAtUtc { get; }

        public bool RecoveredFromBackup { get; }

        public string ErrorCode { get; }

        public long PlayTimeSeconds { get; }

        public string EntryId { get; }

        public string CheckpointId { get; }

        public bool CanLoad => State == SaveSlotState.Valid;

        public static SaveSlotSummary Empty(SaveSlotId slotId)
        {
            return new SaveSlotSummary(slotId, SaveSlotState.Empty, null, null, null, false, null);
        }
    }
}
