using System;

namespace DemonLord.Domain
{
    [Serializable]
    public sealed class SaveEnvelopeDto
    {
        public int schemaVersion;
        public string saveId;
        public string slotId;
        public string createdAtUtc;
        public string updatedAtUtc;
        public string buildVersion;
        public string payloadJson;
        public string payloadSha256;
    }

    [Serializable]
    public sealed class GameSavePayloadDto
    {
        public GameProfileDto profile;
        public GameProgressDto progress;
    }

    [Serializable]
    public sealed class GameProfileDto
    {
        public string profileName;
        public string difficultyId;
        public string tutorialMode;
    }

    [Serializable]
    public sealed class GameProgressDto
    {
        public string entryId;
        public string checkpointId;
        public string areaId;
        public string spawnId;
        public long playTimeSeconds;
    }
}
