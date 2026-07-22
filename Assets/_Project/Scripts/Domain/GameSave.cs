using System;

namespace DemonLord.Domain
{
    public static class SaveSchema
    {
        public const int CurrentVersion = 1;
    }

    public sealed class GameSave
    {
        public GameSave(
            int schemaVersion,
            string saveId,
            SaveSlotId slotId,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            string buildVersion,
            NewGameSettings profile,
            GameEntryPoint progress,
            long playTimeSeconds)
        {
            if (schemaVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            }

            if (!Guid.TryParse(saveId, out _))
            {
                throw new ArgumentException("A save ID must be a GUID.", nameof(saveId));
            }

            if (slotId == null)
            {
                throw new ArgumentNullException(nameof(slotId));
            }

            if (string.IsNullOrWhiteSpace(buildVersion))
            {
                throw new ArgumentException("A build version is required.", nameof(buildVersion));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            if (playTimeSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playTimeSeconds));
            }

            SchemaVersion = schemaVersion;
            SaveId = saveId;
            SlotId = slotId;
            CreatedAtUtc = NormalizeUtc(createdAtUtc);
            UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
            BuildVersion = buildVersion;
            Profile = profile;
            Progress = progress;
            PlayTimeSeconds = playTimeSeconds;
        }

        public int SchemaVersion { get; }

        public string SaveId { get; }

        public SaveSlotId SlotId { get; }

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; }

        public string BuildVersion { get; }

        public NewGameSettings Profile { get; }

        public GameEntryPoint Progress { get; }

        public long PlayTimeSeconds { get; }

        public static GameSave CreateNew(
            SaveSlotId slotId,
            NewGameSettings settings,
            string buildVersion,
            DateTime nowUtc)
        {
            if (!GameEntryPoint.TryCreate(GameEntryPoint.PrologueStartId, "start", out GameEntryPoint entryPoint, out string errorCode))
            {
                throw new InvalidOperationException("The initial entry point is invalid: " + errorCode);
            }

            return new GameSave(
                SaveSchema.CurrentVersion,
                Guid.NewGuid().ToString("D"),
                slotId,
                nowUtc,
                nowUtc,
                buildVersion,
                settings,
                entryPoint,
                0);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
    }
}
