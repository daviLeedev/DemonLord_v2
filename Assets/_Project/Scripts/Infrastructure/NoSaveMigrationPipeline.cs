using System;
using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Infrastructure
{
    public sealed class NoSaveMigrationPipeline : ISaveMigrationPipeline
    {
        public bool TryMigrate(
            SaveEnvelopeDto source,
            int targetSchemaVersion,
            out SaveEnvelopeDto migrated,
            out string errorCode)
        {
            if (source == null)
            {
                migrated = null;
                errorCode = "migration_source_missing";
                return false;
            }

            SaveEnvelopeDto current = source;
            while (current.schemaVersion < targetSchemaVersion)
            {
                if (current.schemaVersion == 1)
                {
                    if (!TryMigrateV1ToV2(current, out current, out errorCode))
                    {
                        migrated = null;
                        return false;
                    }

                    continue;
                }

                if (current.schemaVersion == 2)
                {
                    if (!TryMigrateV2ToV3(current, out current, out errorCode))
                    {
                        migrated = null;
                        return false;
                    }

                    continue;
                }

                migrated = null;
                errorCode = "unsupported_schema_version";
                return false;
            }

            if (current.schemaVersion != targetSchemaVersion)
            {
                migrated = null;
                errorCode = "migration_target_schema_mismatch";
                return false;
            }

            migrated = current;
            errorCode = null;
            return true;
        }

        private static bool TryMigrateV1ToV2(
            SaveEnvelopeDto source,
            out SaveEnvelopeDto migrated,
            out string errorCode)
        {
            try
            {
                LegacyV1PayloadDto legacyPayload = JsonUtility.FromJson<LegacyV1PayloadDto>(source.payloadJson);
                if (legacyPayload == null || legacyPayload.profile == null || legacyPayload.progress == null)
                {
                    migrated = null;
                    errorCode = "v1_payload_invalid";
                    return false;
                }

                LegacyV2PayloadDto payload = new LegacyV2PayloadDto
                {
                    profile = new GameProfileDto
                    {
                        profileName = legacyPayload.profile.profileName,
                        difficultyId = legacyPayload.profile.difficultyId,
                        tutorialMode = legacyPayload.profile.tutorialEnabled ? TutorialMode.DetailValue : TutorialMode.OffValue,
                    },
                    progress = new LegacyV2ProgressDto
                    {
                        entryId = legacyPayload.progress.entryId,
                        checkpointId = legacyPayload.progress.checkpointId,
                        playTimeSeconds = legacyPayload.progress.playTimeSeconds,
                    },
                };
                string payloadJson = JsonUtility.ToJson(payload, false);
                migrated = new SaveEnvelopeDto
                {
                    schemaVersion = 2,
                    saveId = source.saveId,
                    slotId = source.slotId,
                    createdAtUtc = source.createdAtUtc,
                    updatedAtUtc = source.updatedAtUtc,
                    buildVersion = source.buildVersion,
                    payloadJson = payloadJson,
                    payloadSha256 = PayloadChecksum.ComputeSha256(payloadJson),
                };
                errorCode = null;
                return true;
            }
            catch (ArgumentException)
            {
                migrated = null;
                errorCode = "v1_payload_invalid";
                return false;
            }
        }

        private static bool TryMigrateV2ToV3(
            SaveEnvelopeDto source,
            out SaveEnvelopeDto migrated,
            out string errorCode)
        {
            try
            {
                LegacyV2PayloadDto legacyPayload = JsonUtility.FromJson<LegacyV2PayloadDto>(source.payloadJson);
                if (legacyPayload == null || legacyPayload.profile == null || legacyPayload.progress == null)
                {
                    migrated = null;
                    errorCode = "v2_payload_invalid";
                    return false;
                }

                GameSavePayloadDto payload = new GameSavePayloadDto
                {
                    profile = legacyPayload.profile,
                    progress = new GameProgressDto
                    {
                        entryId = legacyPayload.progress.entryId,
                        checkpointId = legacyPayload.progress.checkpointId,
                        areaId = ExplorationAreaIds.WorldAdjustmentLabInterior,
                        spawnId = ExplorationSpawnIds.ReceptionStart,
                        playTimeSeconds = legacyPayload.progress.playTimeSeconds,
                    },
                };
                string payloadJson = JsonUtility.ToJson(payload, false);
                migrated = new SaveEnvelopeDto
                {
                    schemaVersion = 3,
                    saveId = source.saveId,
                    slotId = source.slotId,
                    createdAtUtc = source.createdAtUtc,
                    updatedAtUtc = source.updatedAtUtc,
                    buildVersion = source.buildVersion,
                    payloadJson = payloadJson,
                    payloadSha256 = PayloadChecksum.ComputeSha256(payloadJson),
                };
                errorCode = null;
                return true;
            }
            catch (ArgumentException)
            {
                migrated = null;
                errorCode = "v2_payload_invalid";
                return false;
            }
        }

#pragma warning disable 0649
        [System.Serializable]
        private sealed class LegacyV1PayloadDto
        {
            public LegacyV1ProfileDto profile;
            public GameProgressDto progress;
        }

        [System.Serializable]
        private sealed class LegacyV1ProfileDto
        {
            public string profileName;
            public string difficultyId;
            public bool tutorialEnabled;
        }

        [System.Serializable]
        private sealed class LegacyV2PayloadDto
        {
            public GameProfileDto profile;
            public LegacyV2ProgressDto progress;
        }

        [System.Serializable]
        private sealed class LegacyV2ProgressDto
        {
            public string entryId;
            public string checkpointId;
            public long playTimeSeconds;
        }
#pragma warning restore 0649
    }
}
