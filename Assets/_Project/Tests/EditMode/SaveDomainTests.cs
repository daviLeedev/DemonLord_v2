using System;
using DemonLord.Domain;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class SaveDomainTests
    {
        [TestCase(" story ", "story")]
        [TestCase("normal", "normal")]
        [TestCase("hard", "hard")]
        public void NewGameSettings_NormalizesAndAcceptsSupportedValues(string profileName, string difficultyId)
        {
            bool created = NewGameSettings.TryCreate(profileName, difficultyId, true, out NewGameSettings settings, out string errorCode);

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
            bool created = NewGameSettings.TryCreate(profileName, DifficultyId.NormalValue, true, out NewGameSettings settings, out string errorCode);

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
            Assert.That(NewGameSettings.TryCreate("  마왕  ", DifficultyId.NormalValue, true, out NewGameSettings settings, out string errorCode), Is.True, errorCode);

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
                    tutorialEnabled = true,
                },
                progress = new GameProgressDto
                {
                    entryId = GameEntryPoint.PrologueStartId,
                    checkpointId = "start",
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
    }
}
