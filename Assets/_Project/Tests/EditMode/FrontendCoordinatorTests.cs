using System;
using System.Collections.Generic;
using DemonLord.Application;
using DemonLord.Domain;
using DemonLord.Infrastructure;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class FrontendCoordinatorTests
    {
        [Test]
        public void NewGameFlow_CreatesInitialSaveAndResolvesGameShell()
        {
            FakeSaveRepository repository = new FakeSaveRepository();
            FrontendCoordinator coordinator = CreateCoordinator(repository, new EntryPointResolver(), out InMemoryPlayerSession session);
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId);
            NewGameSettings.TryCreate("Demon Lord", DifficultyId.NormalValue, true, out NewGameSettings settings, out string errorCode);

            Assert.That(coordinator.CompleteLogoNotice(), Is.True);
            Assert.That(coordinator.CompleteTitleIntro(), Is.True);
            Assert.That(coordinator.OpenStartMode(), Is.True);
            Assert.That(coordinator.OpenNewGameSlots().Accepted, Is.True);
            Assert.That(coordinator.SelectSlot(slotId).Accepted, Is.True);
            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.NewGameSetup));

            FrontendCommandResult result = coordinator.CreateSelectedNewGame(settings, "0.1.0");

            Assert.That(result.HasEntryDestination, Is.True);
            Assert.That(result.Destination.SceneKey, Is.EqualTo("90_GameShell"));
            Assert.That(result.Destination.SpawnKey, Is.EqualTo("start"));
            Assert.That(session.CurrentSave, Is.Not.Null);
            Assert.That(session.CurrentSave.Progress.EntryId, Is.EqualTo(GameEntryPoint.PrologueStartId));
            Assert.That(session.CurrentSave.Progress.CheckpointId, Is.EqualTo("start"));
        }

        [Test]
        public void FailedLoad_ShowsErrorAndDoesNotChangeSession()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot02Value, out SaveSlotId slotId);
            FakeSaveRepository repository = new FakeSaveRepository
            {
                LoadResult = SaveReadResult.Failure(SaveReadStatus.Corrupt, "payload_checksum_mismatch", null),
                Summaries = new[]
                {
                    new SaveSlotSummary(slotId, SaveSlotState.Valid, "Existing", DifficultyId.NormalValue, DateTime.UtcNow, false, null),
                },
            };
            FrontendCoordinator coordinator = CreateCoordinator(repository, new EntryPointResolver(), out InMemoryPlayerSession session);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            coordinator.OpenStartMode();
            coordinator.OpenContinueSlots();
            FrontendCommandResult result = coordinator.SelectSlot(slotId);

            Assert.That(result.Accepted, Is.False);
            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.ErrorDialog));
            Assert.That(coordinator.ErrorCode, Is.EqualTo("payload_checksum_mismatch"));
            Assert.That(session.CurrentSave, Is.Null);
            Assert.That(coordinator.Back(), Is.True);
            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.SaveSlotsLoad));
        }

        private static FrontendCoordinator CreateCoordinator(
            FakeSaveRepository repository,
            IEntryPointResolver resolver,
            out InMemoryPlayerSession session)
        {
            session = new InMemoryPlayerSession();
            return new FrontendCoordinator(
                new ListSaveSlotsUseCase(repository),
                new CreateNewGameUseCase(repository, new FixedClock()),
                new LoadGameUseCase(repository),
                session,
                resolver);
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed class FakeSaveRepository : ISaveRepository
        {
            public IReadOnlyList<SaveSlotSummary> Summaries { get; set; } = CreateEmptySummaries();

            public SaveReadResult LoadResult { get; set; }

            public GameSave SavedGame { get; private set; }

            public IReadOnlyList<SaveSlotSummary> ListSlots()
            {
                return Summaries;
            }

            public SaveReadResult Load(SaveSlotId slotId)
            {
                return LoadResult ?? SaveReadResult.Failure(SaveReadStatus.Empty, "save_not_found", null);
            }

            public SaveWriteResult Save(GameSave save)
            {
                SavedGame = save;
                return SaveWriteResult.Success();
            }

            public SaveWriteResult Delete(SaveSlotId slotId)
            {
                return SaveWriteResult.Success();
            }

            private static IReadOnlyList<SaveSlotSummary> CreateEmptySummaries()
            {
                List<SaveSlotSummary> summaries = new List<SaveSlotSummary>();
                foreach (string value in SaveSlotId.AllValues)
                {
                    SaveSlotId.TryCreate(value, out SaveSlotId slotId);
                    summaries.Add(SaveSlotSummary.Empty(slotId));
                }

                return summaries;
            }
        }
    }
}
