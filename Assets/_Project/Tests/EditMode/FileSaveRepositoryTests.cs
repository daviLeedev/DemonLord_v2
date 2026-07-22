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
                NewGameSettings.TryCreate(profileName, DifficultyId.NormalValue, true, out NewGameSettings settings, out string errorCode),
                Is.True,
                errorCode);
            return GameSave.CreateNew(slotId, settings, "0.1.0", DateTime.UtcNow);
        }

        private string GetSavePath(string slotId, string fileName)
        {
            return Path.Combine(temporaryDataPath, "Saves", slotId, fileName);
        }
    }
}
