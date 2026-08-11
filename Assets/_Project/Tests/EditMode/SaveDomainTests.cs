using System;
using System.Collections.Generic;
using DemonLord.Application;
using DemonLord.Domain;
using DemonLord.Infrastructure;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class SaveDomainTests
    {
        [TestCase(LabCheckpointId.Start)]
        [TestCase(LabCheckpointId.ResearcherBriefed)]
        [TestCase(LabCheckpointId.TaxLedgerReviewed)]
        [TestCase(LabCheckpointId.ArchiveCatalogued)]
        [TestCase(LabCheckpointId.CombatLiaisonBriefed)]
        public void LabCheckpointId_AcceptsEveryPersistedLaboratoryStage(string checkpointId)
        {
            Assert.That(LabCheckpointId.IsKnown(checkpointId), Is.True);
        }

        [TestCase(" story ", "story")]
        [TestCase("normal", "normal")]
        [TestCase("hard", "hard")]
        public void NewGameSettings_NormalizesAndAcceptsSupportedValues(string profileName, string difficultyId)
        {
            bool created = NewGameSettings.TryCreate(profileName, difficultyId, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode);

            Assert.That(created, Is.True, errorCode);
            Assert.That(settings.ProfileName, Is.EqualTo(profileName.Trim()));
            Assert.That(settings.DifficultyId.Value, Is.EqualTo(difficultyId));
        }

        [TestCase("")]
        [TestCase("                 ")]
        [TestCase("This profile name is too long")]
        [TestCase("bad/name")]
        [TestCase("bad\nname")]
        public void NewGameSettings_RejectsUnsafeProfileNames(string profileName)
        {
            bool created = NewGameSettings.TryCreate(profileName, DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode);

            Assert.That(created, Is.False);
            Assert.That(settings, Is.Null);
            Assert.That(errorCode, Does.StartWith("invalid_profile_name"));
        }

        [Test]
        public void SaveSlotId_OnlyAcceptsTheThreeFixedSlots()
        {
            Assert.That(SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId firstSlot), Is.True);
            Assert.That(firstSlot.Value, Is.EqualTo(SaveSlotId.Slot01Value));
            Assert.That(SaveSlotId.TryCreate("slot-04", out _), Is.False);
        }

        [Test]
        public void PayloadChecksum_DetectsPayloadChanges()
        {
            const string payload = "{\"progress\":{\"entryId\":\"prologue_start\"}}";
            string checksum = PayloadChecksum.ComputeSha256(payload);

            Assert.That(PayloadChecksum.Matches(payload, checksum), Is.True);
            Assert.That(PayloadChecksum.Matches(payload + " ", checksum), Is.False);
        }

        [Test]
        public void GameSaveMapper_RoundTripsValidNewGameData()
        {
            Assert.That(SaveSlotId.TryCreate(SaveSlotId.Slot02Value, out SaveSlotId slotId), Is.True);
            Assert.That(NewGameSettings.TryCreate("  마왕  ", DifficultyId.NormalValue, TutorialMode.CoreValue, out NewGameSettings settings, out string errorCode), Is.True, errorCode);

            DateTime createdAtUtc = new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);
            GameSave original = GameSave.CreateNew(slotId, settings, "0.1.0", createdAtUtc);
            GameSavePayloadDto payload = GameSaveMapper.ToPayloadDto(original);
            SaveEnvelopeDto envelope = new SaveEnvelopeDto
            {
                schemaVersion = original.SchemaVersion,
                saveId = original.SaveId,
                slotId = original.SlotId.Value,
                buildVersion = original.BuildVersion,
            };

            bool mapped = GameSaveMapper.TryFromPayloadDto(
                envelope,
                payload,
                original.CreatedAtUtc,
                original.UpdatedAtUtc,
                out GameSave mappedSave,
                out errorCode);

            Assert.That(mapped, Is.True, errorCode);
            Assert.That(mappedSave.Profile.ProfileName, Is.EqualTo("마왕"));
            Assert.That(mappedSave.Progress.EntryId, Is.EqualTo(GameEntryPoint.PrologueStartId));
            Assert.That(mappedSave.Progress.CheckpointId, Is.EqualTo("start"));
            Assert.That(mappedSave.PlayTimeSeconds, Is.Zero);
            Assert.That(mappedSave.Profile.TutorialMode.Value, Is.EqualTo(TutorialMode.CoreValue));
            Assert.That(mappedSave.Location.AreaId.Value, Is.EqualTo(ExplorationAreaIds.WorldAdjustmentLabInterior));
            Assert.That(mappedSave.Location.SpawnId.Value, Is.EqualTo(ExplorationSpawnIds.ReceptionStart));
        }

        [Test]
        public void GameSaveMapper_RejectsUnknownSlotIds()
        {
            SaveEnvelopeDto envelope = new SaveEnvelopeDto
            {
                schemaVersion = SaveSchema.CurrentVersion,
                saveId = Guid.NewGuid().ToString("D"),
                slotId = "slot-99",
                buildVersion = "0.1.0",
            };
            GameSavePayloadDto payload = new GameSavePayloadDto
            {
                profile = new GameProfileDto
                {
                    profileName = "마왕",
                    difficultyId = DifficultyId.NormalValue,
                    tutorialMode = TutorialMode.DetailValue,
                },
                progress = new GameProgressDto
                {
                    entryId = GameEntryPoint.PrologueStartId,
                    checkpointId = "start",
                    areaId = ExplorationAreaIds.WorldAdjustmentLabInterior,
                    spawnId = ExplorationSpawnIds.ReceptionStart,
                    playTimeSeconds = 0,
                },
            };

            bool mapped = GameSaveMapper.TryFromPayloadDto(
                envelope,
                payload,
                DateTime.UtcNow,
                DateTime.UtcNow,
                out GameSave save,
                out string errorCode);

            Assert.That(mapped, Is.False);
            Assert.That(save, Is.Null);
            Assert.That(errorCode, Is.EqualTo("invalid_slot_id"));
        }

        [Test]
        public void SaveReadResult_RequiresSaveForSuccess()
        {
            Assert.That(
                () => SaveReadResult.Success(null, false),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                () => SaveReadResult.Failure(SaveReadStatus.Success, "unexpected", "unexpected"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(TutorialMode.DetailValue)]
        [TestCase(TutorialMode.CoreValue)]
        [TestCase(TutorialMode.OffValue)]
        public void NewGameSettings_AcceptsAllTutorialModes(string tutorialMode)
        {
            bool created = NewGameSettings.TryCreate("마왕", DifficultyId.NormalValue, tutorialMode, out NewGameSettings settings, out string errorCode);

            Assert.That(created, Is.True, errorCode);
            Assert.That(settings.TutorialMode.Value, Is.EqualTo(tutorialMode));
        }

        [Test]
        public void NewGameSettings_RejectsUnknownTutorialMode()
        {
            bool created = NewGameSettings.TryCreate("마왕", DifficultyId.NormalValue, "enabled", out NewGameSettings settings, out string errorCode);

            Assert.That(created, Is.False);
            Assert.That(settings, Is.Null);
            Assert.That(errorCode, Is.EqualTo("invalid_tutorial_mode"));
        }

        [Test]
        public void ManualCheckpointSave_SuccessUpdatesSessionWithoutChangingSlotOrEntry()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId);
            NewGameSettings.TryCreate("Tester", DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode);
            DateTime createdAt = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
            GameSave original = GameSave.CreateNew(slotId, settings, "test", createdAt);
            InMemoryPlayerSession session = new InMemoryPlayerSession();
            session.SetCurrentSave(original);
            RecordingSaveRepository repository = new RecordingSaveRepository(SaveWriteResult.Success());
            DateTime savedAt = createdAt.AddMinutes(5);

            SaveWriteResult result = new SaveGameProgressUseCase(repository, new FixedClock(savedAt))
                .Execute(session, original.Progress.CheckpointId);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(repository.LastSaved, Is.Not.Null);
            Assert.That(repository.LastSaved.SlotId, Is.EqualTo(original.SlotId));
            Assert.That(repository.LastSaved.Progress.EntryId, Is.EqualTo(original.Progress.EntryId));
            Assert.That(repository.LastSaved.Progress.CheckpointId, Is.EqualTo(original.Progress.CheckpointId));
            Assert.That(session.CurrentSave, Is.SameAs(repository.LastSaved));
            Assert.That(session.CurrentSave.UpdatedAtUtc, Is.EqualTo(savedAt));
        }

        [Test]
        public void ManualCheckpointSave_FailureKeepsCurrentSessionSnapshot()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot02Value, out SaveSlotId slotId);
            NewGameSettings.TryCreate("Tester", DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode);
            GameSave original = GameSave.CreateNew(slotId, settings, "test", DateTime.UtcNow);
            InMemoryPlayerSession session = new InMemoryPlayerSession();
            session.SetCurrentSave(original);
            RecordingSaveRepository repository = new RecordingSaveRepository(
                SaveWriteResult.Failure(SaveWriteStatus.IoFailure, "write_failed", "test failure"));

            SaveWriteResult result = new SaveGameProgressUseCase(repository, new FixedClock(DateTime.UtcNow.AddMinutes(1)))
                .Execute(session, original.Progress.CheckpointId);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(session.CurrentSave, Is.SameAs(original));
            Assert.That(repository.LastSaved, Is.Not.Null);
        }

        [Test]
        public void ManualCheckpointSave_PersistsExplicitAreaAndSpawn()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId);
            NewGameSettings.TryCreate("Tester", DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out _);
            GameSave original = GameSave.CreateNew(slotId, settings, "test", DateTime.UtcNow);
            InMemoryPlayerSession session = new InMemoryPlayerSession();
            session.SetCurrentSave(original);
            RecordingSaveRepository repository = new RecordingSaveRepository(SaveWriteResult.Success());
            ExplorationLocation.TryCreate(
                ExplorationAreaIds.BureauCourtyard,
                ExplorationSpawnIds.LabExit,
                out ExplorationLocation location,
                out _);

            SaveWriteResult result = new SaveGameProgressUseCase(repository, new FixedClock(DateTime.UtcNow))
                .Execute(session, original.Progress.CheckpointId, location);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(repository.LastSaved.Location, Is.EqualTo(location));
            Assert.That(session.CurrentSave.Location, Is.EqualTo(location));
        }

        private sealed class FixedClock : IClock
        {
            public FixedClock(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTime UtcNow { get; }
        }

        private sealed class RecordingSaveRepository : ISaveRepository
        {
            private readonly SaveWriteResult writeResult;

            public RecordingSaveRepository(SaveWriteResult writeResult)
            {
                this.writeResult = writeResult;
            }

            public GameSave LastSaved { get; private set; }

            public IReadOnlyList<SaveSlotSummary> ListSlots()
            {
                return Array.Empty<SaveSlotSummary>();
            }

            public SaveReadResult Load(SaveSlotId slotId)
            {
                return SaveReadResult.Failure(SaveReadStatus.Empty, "not_used", null);
            }

            public SaveWriteResult Save(GameSave save)
            {
                LastSaved = save;
                return writeResult;
            }

            public SaveWriteResult Delete(SaveSlotId slotId)
            {
                return SaveWriteResult.Success();
            }
        }
    }
}
