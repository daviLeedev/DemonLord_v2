using System;
using System.IO;
using DemonLord.Application;
using DemonLord.Domain;
using DemonLord.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace DemonLord.Tests.EditMode
{
    public sealed class FileSaveRepositoryTests
    {
        private string temporaryDataPath;
        private FileSaveRepository repository;

        [SetUp]
        public void SetUp()
        {
            temporaryDataPath = Path.Combine(Path.GetTempPath(), "DemonLord_v2_SaveTests", Guid.NewGuid().ToString("N"));
            repository = new FileSaveRepository(
                temporaryDataPath,
                new UnityJsonSaveSerializer(),
                new NoSaveMigrationPipeline());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDataPath))
            {
                Directory.Delete(temporaryDataPath, true);
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsValidDataAndListsTheSlot()
        {
            GameSave original = CreateNewSave(SaveSlotId.Slot01Value, "First Lord");

            SaveWriteResult writeResult = repository.Save(original);
            SaveReadResult readResult = repository.Load(original.SlotId);

            Assert.That(writeResult.IsSuccess, Is.True, writeResult.DiagnosticMessage);
            Assert.That(readResult.IsSuccess, Is.True, readResult.DiagnosticMessage);
            Assert.That(readResult.RecoveredFromBackup, Is.False);
            Assert.That(readResult.Save.SaveId, Is.EqualTo(original.SaveId));
            Assert.That(readResult.Save.Profile.ProfileName, Is.EqualTo("First Lord"));
            Assert.That(readResult.Save.Progress.EntryId, Is.EqualTo(GameEntryPoint.PrologueStartId));
            Assert.That(readResult.Save.Location, Is.EqualTo(ExplorationLocation.Initial));

            SaveSlotSummary firstSlot = repository.ListSlots()[0];
            Assert.That(firstSlot.State, Is.EqualTo(SaveSlotState.Valid));
            Assert.That(firstSlot.ProfileName, Is.EqualTo("First Lord"));
        }

        [Test]
        public void Load_RecoversFromBackupWhenPrimaryIsCorrupt()
        {
            GameSave firstSave = CreateNewSave(SaveSlotId.Slot02Value, "Backup Lord");
            GameSave replacementSave = CreateNewSave(SaveSlotId.Slot02Value, "Current Lord");

            Assert.That(repository.Save(firstSave).IsSuccess, Is.True);
            Assert.That(repository.Save(replacementSave).IsSuccess, Is.True);

            string primaryPath = GetSavePath(SaveSlotId.Slot02Value, "save.json");
            File.WriteAllText(primaryPath, "{ invalid json");

            SaveReadResult result = repository.Load(replacementSave.SlotId);

            Assert.That(result.IsSuccess, Is.True, result.DiagnosticMessage);
            Assert.That(result.RecoveredFromBackup, Is.True);
            Assert.That(result.Save.Profile.ProfileName, Is.EqualTo("Backup Lord"));
        }

        [Test]
        public void Load_RejectsChecksumMismatchAsCorrupt()
        {
            GameSave save = CreateNewSave(SaveSlotId.Slot03Value, "Checksum Lord");
            Assert.That(repository.Save(save).IsSuccess, Is.True);

            string primaryPath = GetSavePath(SaveSlotId.Slot03Value, "save.json");
            SaveEnvelopeDto envelope = JsonUtility.FromJson<SaveEnvelopeDto>(File.ReadAllText(primaryPath));
            envelope.payloadSha256 = new string('0', 64);
            File.WriteAllText(primaryPath, JsonUtility.ToJson(envelope));

            SaveReadResult result = repository.Load(save.SlotId);

            Assert.That(result.Status, Is.EqualTo(SaveReadStatus.Corrupt));
            Assert.That(result.ErrorCode, Is.EqualTo("payload_checksum_mismatch"));
        }

        [Test]
        public void Load_RejectsFutureSchemaAsIncompatible()
        {
            GameSave save = CreateNewSave(SaveSlotId.Slot01Value, "Future Lord");
            Assert.That(repository.Save(save).IsSuccess, Is.True);

            string primaryPath = GetSavePath(SaveSlotId.Slot01Value, "save.json");
            SaveEnvelopeDto envelope = JsonUtility.FromJson<SaveEnvelopeDto>(File.ReadAllText(primaryPath));
            envelope.schemaVersion = SaveSchema.CurrentVersion + 1;
            File.WriteAllText(primaryPath, JsonUtility.ToJson(envelope));

            SaveReadResult result = repository.Load(save.SlotId);

            Assert.That(result.Status, Is.EqualTo(SaveReadStatus.Incompatible));
            Assert.That(result.ErrorCode, Is.EqualTo("future_schema_version"));
        }

        [TestCase(true, TutorialMode.DetailValue)]
        [TestCase(false, TutorialMode.OffValue)]
        public void Load_MigratesV1TutorialBooleanWithoutOverwritingSource(bool tutorialEnabled, string expectedTutorialMode)
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId);
            string primaryPath = GetSavePath(SaveSlotId.Slot01Value, "save.json");
            Directory.CreateDirectory(Path.GetDirectoryName(primaryPath));
            File.WriteAllText(primaryPath, CreateLegacyV1Json(slotId, tutorialEnabled));

            SaveReadResult result = repository.Load(slotId);

            Assert.That(result.IsSuccess, Is.True, result.DiagnosticMessage);
            Assert.That(result.Save.SchemaVersion, Is.EqualTo(SaveSchema.CurrentVersion));
            Assert.That(result.Save.Profile.TutorialMode.Value, Is.EqualTo(expectedTutorialMode));
            SaveEnvelopeDto originalEnvelope = JsonUtility.FromJson<SaveEnvelopeDto>(File.ReadAllText(primaryPath));
            Assert.That(originalEnvelope.schemaVersion, Is.EqualTo(1));
        }

        [Test]
        public void Load_MigratesV2ToV3WithDeterministicInitialLocation()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot02Value, out SaveSlotId slotId);
            string primaryPath = GetSavePath(slotId.Value, "save.json");
            Directory.CreateDirectory(Path.GetDirectoryName(primaryPath));
            File.WriteAllText(primaryPath, CreateLegacyV2Json(slotId));

            SaveReadResult result = repository.Load(slotId);

            Assert.That(result.IsSuccess, Is.True, result.DiagnosticMessage);
            Assert.That(result.Save.SchemaVersion, Is.EqualTo(3));
            Assert.That(result.Save.Location.AreaId.Value, Is.EqualTo(ExplorationAreaIds.WorldAdjustmentLabInterior));
            Assert.That(result.Save.Location.SpawnId.Value, Is.EqualTo(ExplorationSpawnIds.ReceptionStart));
            SaveEnvelopeDto source = JsonUtility.FromJson<SaveEnvelopeDto>(File.ReadAllText(primaryPath));
            Assert.That(source.schemaVersion, Is.EqualTo(2));
        }

        [Test]
        public void Delete_RemovesPrimaryAndBackupData()
        {
            GameSave firstSave = CreateNewSave(SaveSlotId.Slot03Value, "Delete First");
            GameSave replacementSave = CreateNewSave(SaveSlotId.Slot03Value, "Delete Current");
            Assert.That(repository.Save(firstSave).IsSuccess, Is.True);
            Assert.That(repository.Save(replacementSave).IsSuccess, Is.True);

            SaveWriteResult deleteResult = repository.Delete(replacementSave.SlotId);
            SaveReadResult readResult = repository.Load(replacementSave.SlotId);

            Assert.That(deleteResult.IsSuccess, Is.True, deleteResult.DiagnosticMessage);
            Assert.That(readResult.Status, Is.EqualTo(SaveReadStatus.Empty));
        }

        private GameSave CreateNewSave(string slotValue, string profileName)
        {
            Assert.That(SaveSlotId.TryCreate(slotValue, out SaveSlotId slotId), Is.True);
            Assert.That(
                NewGameSettings.TryCreate(profileName, DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode),
                Is.True,
                errorCode);
            return GameSave.CreateNew(slotId, settings, "0.1.0", DateTime.UtcNow);
        }

        private string GetSavePath(string slotId, string fileName)
        {
            return Path.Combine(temporaryDataPath, "Saves", slotId, fileName);
        }

        private static string CreateLegacyV1Json(SaveSlotId slotId, bool tutorialEnabled)
        {
            LegacyV1Payload payload = new LegacyV1Payload
            {
                profile = new LegacyV1Profile
                {
                    profileName = "Legacy Lord",
                    difficultyId = DifficultyId.NormalValue,
                    tutorialEnabled = tutorialEnabled,
                },
                progress = new GameProgressDto
                {
                    entryId = GameEntryPoint.PrologueStartId,
                    checkpointId = "start",
                    playTimeSeconds = 0,
                },
            };
            string payloadJson = JsonUtility.ToJson(payload, false);
            SaveEnvelopeDto envelope = new SaveEnvelopeDto
            {
                schemaVersion = 1,
                saveId = Guid.NewGuid().ToString("D"),
                slotId = slotId.Value,
                createdAtUtc = DateTime.UtcNow.ToString("O"),
                updatedAtUtc = DateTime.UtcNow.ToString("O"),
                buildVersion = "0.1.0",
                payloadJson = payloadJson,
                payloadSha256 = PayloadChecksum.ComputeSha256(payloadJson),
            };
            return JsonUtility.ToJson(envelope, false);
        }

        private static string CreateLegacyV2Json(SaveSlotId slotId)
        {
            LegacyV2Payload payload = new LegacyV2Payload
            {
                profile = new GameProfileDto
                {
                    profileName = "V2 Lord",
                    difficultyId = DifficultyId.NormalValue,
                    tutorialMode = TutorialMode.DetailValue,
                },
                progress = new LegacyV2Progress
                {
                    entryId = GameEntryPoint.PrologueStartId,
                    checkpointId = "start",
                    playTimeSeconds = 12,
                },
            };
            string payloadJson = JsonUtility.ToJson(payload, false);
            SaveEnvelopeDto envelope = new SaveEnvelopeDto
            {
                schemaVersion = 2,
                saveId = Guid.NewGuid().ToString("D"),
                slotId = slotId.Value,
                createdAtUtc = DateTime.UtcNow.ToString("O"),
                updatedAtUtc = DateTime.UtcNow.ToString("O"),
                buildVersion = "0.1.0",
                payloadJson = payloadJson,
                payloadSha256 = PayloadChecksum.ComputeSha256(payloadJson),
            };
            return JsonUtility.ToJson(envelope, false);
        }

        [Serializable]
        private sealed class LegacyV1Payload
        {
            public LegacyV1Profile profile;
            public GameProgressDto progress;
        }

        [Serializable]
        private sealed class LegacyV1Profile
        {
            public string profileName;
            public string difficultyId;
            public bool tutorialEnabled;
        }

        [Serializable]
        private sealed class LegacyV2Payload
        {
            public GameProfileDto profile;
            public LegacyV2Progress progress;
        }

        [Serializable]
        private sealed class LegacyV2Progress
        {
            public string entryId;
            public string checkpointId;
            public long playTimeSeconds;
        }
    }
}
