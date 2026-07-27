using System;

namespace DemonLord.Domain
{
    public static class SaveSchema
    {
        public const int CurrentVersion = 3;
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
            ExplorationLocation location,
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

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
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
            Location = location;
            PlayTimeSeconds = playTimeSeconds;
        }

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
            : this(
                schemaVersion,
                saveId,
                slotId,
                createdAtUtc,
                updatedAtUtc,
                buildVersion,
                profile,
                progress,
                ExplorationLocation.Initial,
                playTimeSeconds)
        {
        }

        public int SchemaVersion { get; }

        public string SaveId { get; }

        public SaveSlotId SlotId { get; }

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; }

        public string BuildVersion { get; }

        public NewGameSettings Profile { get; }

        public GameEntryPoint Progress { get; }

        public ExplorationLocation Location { get; }

        public long PlayTimeSeconds { get; }

        /// <summary>
        /// Returns a new immutable save snapshot at the requested in-world checkpoint.
        /// The slot and player profile remain untouched so a gameplay checkpoint cannot
        /// accidentally become a new-save operation.
        /// </summary>
        public GameSave WithProgress(GameEntryPoint progress, DateTime updatedAtUtc)
        {
            return WithProgress(progress, Location, updatedAtUtc);
        }

        public GameSave WithProgress(
            GameEntryPoint progress,
            ExplorationLocation location,
            DateTime updatedAtUtc)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return new GameSave(
                SchemaVersion,
                SaveId,
                SlotId,
                CreatedAtUtc,
                updatedAtUtc,
                BuildVersion,
                Profile,
                progress,
                location,
                PlayTimeSeconds);
        }

        public static GameSave CreateNew(
            SaveSlotId slotId,
            NewGameSettings settings,
            string buildVersion,
            DateTime nowUtc)
        {
            if (!GameEntryPoint.TryCreate(GameEntryPoint.PrologueStartId, LabCheckpointId.Start, out GameEntryPoint entryPoint, out string errorCode))
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
                ExplorationLocation.Initial,
                0);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
    }
}
