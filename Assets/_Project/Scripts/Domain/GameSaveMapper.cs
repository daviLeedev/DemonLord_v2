using System;

namespace DemonLord.Domain
{
    public static class GameSaveMapper
    {
        public static GameSavePayloadDto ToPayloadDto(GameSave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            return new GameSavePayloadDto
            {
                profile = new GameProfileDto
                {
                    profileName = save.Profile.ProfileName,
                    difficultyId = save.Profile.DifficultyId.Value,
                    tutorialMode = save.Profile.TutorialMode.Value,
                },
                progress = new GameProgressDto
                {
                    entryId = save.Progress.EntryId,
                    checkpointId = save.Progress.CheckpointId,
                    areaId = save.Location.AreaId.Value,
                    spawnId = save.Location.SpawnId.Value,
                    playTimeSeconds = save.PlayTimeSeconds,
                },
            };
        }

        public static bool TryFromPayloadDto(
            SaveEnvelopeDto envelope,
            GameSavePayloadDto payload,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            out GameSave save,
            out string errorCode)
        {
            if (envelope == null || payload == null || payload.profile == null || payload.progress == null)
            {
                save = null;
                errorCode = "missing_save_data";
                return false;
            }

            if (!SaveSlotId.TryCreate(envelope.slotId, out SaveSlotId slotId))
            {
                save = null;
                errorCode = "invalid_slot_id";
                return false;
            }

            if (!NewGameSettings.TryCreate(
                    payload.profile.profileName,
                    payload.profile.difficultyId,
                    payload.profile.tutorialMode,
                    out NewGameSettings profile,
                    out errorCode))
            {
                save = null;
                return false;
            }

            if (!GameEntryPoint.TryCreate(
                    payload.progress.entryId,
                    payload.progress.checkpointId,
                    out GameEntryPoint progress,
                    out errorCode))
            {
                save = null;
                return false;
            }

            if (!ExplorationLocation.TryCreate(
                    payload.progress.areaId,
                    payload.progress.spawnId,
                    out ExplorationLocation location,
                    out errorCode))
            {
                save = null;
                return false;
            }

            try
            {
                save = new GameSave(
                    envelope.schemaVersion,
                    envelope.saveId,
                    slotId,
                    createdAtUtc,
                    updatedAtUtc,
                    envelope.buildVersion,
                    profile,
                    progress,
                    location,
                    payload.progress.playTimeSeconds);
                errorCode = null;
                return true;
            }
            catch (ArgumentException)
            {
                save = null;
                errorCode = "invalid_save_header";
                return false;
            }
        }
    }
}
