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
            string errorCode)
        {
            SlotId = slotId ?? throw new ArgumentNullException(nameof(slotId));
            State = state;
            ProfileName = profileName;
            DifficultyId = difficultyId;
            UpdatedAtUtc = updatedAtUtc;
            RecoveredFromBackup = recoveredFromBackup;
            ErrorCode = errorCode;
        }

        public SaveSlotId SlotId { get; }

        public SaveSlotState State { get; }

        public string ProfileName { get; }

        public string DifficultyId { get; }

        public DateTime? UpdatedAtUtc { get; }

        public bool RecoveredFromBackup { get; }

        public string ErrorCode { get; }

        public bool CanLoad => State == SaveSlotState.Valid;

        public static SaveSlotSummary Empty(SaveSlotId slotId)
        {
            return new SaveSlotSummary(slotId, SaveSlotState.Empty, null, null, null, false, null);
        }
    }
}
