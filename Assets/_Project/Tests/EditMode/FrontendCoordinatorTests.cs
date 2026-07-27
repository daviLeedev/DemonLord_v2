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
            NewGameSettings.TryCreate("Demon Lord", DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode);

            Assert.That(coordinator.CompleteLogoNotice(), Is.True);
            Assert.That(coordinator.CompleteTitleIntro(), Is.True);
            Assert.That(coordinator.OpenStartMode(), Is.True);
            Assert.That(coordinator.OpenNewGameSlots().Accepted, Is.True);
            Assert.That(coordinator.SelectSlot(slotId).Accepted, Is.True);
            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.NewGameSetup));

            FrontendCommandResult result = coordinator.CreateSelectedNewGame(settings, "0.1.0");

            Assert.That(result.HasEntryDestination, Is.True);
            Assert.That(result.Destination.SceneKey, Is.EqualTo("90_GameShell"));
            Assert.That(result.Destination.AreaId, Is.EqualTo(ExplorationAreaIds.WorldAdjustmentLabInterior));
            Assert.That(result.Destination.SpawnKey, Is.EqualTo(ExplorationSpawnIds.ReceptionStart));
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

        [Test]
        public void ContinueLatest_UsesMostRecentlyUpdatedValidSlot()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId firstSlot);
            SaveSlotId.TryCreate(SaveSlotId.Slot02Value, out SaveSlotId secondSlot);
            DateTime firstUpdatedAt = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
            DateTime secondUpdatedAt = firstUpdatedAt.AddMinutes(30);
            GameSave secondSave = CreateSave(secondSlot, secondUpdatedAt);
            FakeSaveRepository repository = new FakeSaveRepository
            {
                Summaries = new[]
                {
                    new SaveSlotSummary(firstSlot, SaveSlotState.Valid, "First", DifficultyId.NormalValue, firstUpdatedAt, false, null),
                    new SaveSlotSummary(secondSlot, SaveSlotState.Valid, "Second", DifficultyId.HardValue, secondUpdatedAt, false, null),
                },
            };
            repository.LoadResults[secondSlot.Value] = SaveReadResult.Success(secondSave, false);
            FrontendCoordinator coordinator = CreateCoordinator(repository, new EntryPointResolver(), out InMemoryPlayerSession session);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            FrontendCommandResult result = coordinator.ContinueLatest();

            Assert.That(result.HasEntryDestination, Is.True);
            Assert.That(repository.LastLoadedSlot, Is.EqualTo(secondSlot));
            Assert.That(session.CurrentSave, Is.EqualTo(secondSave));
        }

        [Test]
        public void ContinueLatest_UsesSlotIdAsDeterministicTieBreak()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId firstSlot);
            SaveSlotId.TryCreate(SaveSlotId.Slot02Value, out SaveSlotId secondSlot);
            DateTime updatedAt = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
            GameSave firstSave = CreateSave(firstSlot, updatedAt);
            FakeSaveRepository repository = new FakeSaveRepository
            {
                Summaries = new[]
                {
                    new SaveSlotSummary(secondSlot, SaveSlotState.Valid, "Second", DifficultyId.NormalValue, updatedAt, false, null),
                    new SaveSlotSummary(firstSlot, SaveSlotState.Valid, "First", DifficultyId.NormalValue, updatedAt, false, null),
                },
            };
            repository.LoadResults[firstSlot.Value] = SaveReadResult.Success(firstSave, false);
            FrontendCoordinator coordinator = CreateCoordinator(repository, new EntryPointResolver(), out _);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            FrontendCommandResult result = coordinator.ContinueLatest();

            Assert.That(result.HasEntryDestination, Is.True);
            Assert.That(repository.LastLoadedSlot, Is.EqualTo(firstSlot));
        }

        [Test]
        public void SettingsBack_ReturnsToMainMenu()
        {
            FrontendCoordinator coordinator = CreateCoordinator(new FakeSaveRepository(), new EntryPointResolver(), out _);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            Assert.That(coordinator.OpenSettings(), Is.True);
            Assert.That(coordinator.Back(), Is.True);

            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.MainMenu));
        }

        [Test]
        public void CancelExitConfirmation_ReturnsToMainMenu()
        {
            FrontendCoordinator coordinator = CreateCoordinator(new FakeSaveRepository(), new EntryPointResolver(), out _);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            Assert.That(coordinator.OpenExitConfirmation(), Is.True);
            Assert.That(coordinator.ConfirmExit(false), Is.True);

            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.MainMenu));
        }

        [Test]
        public void SaveSlotsBack_ReturnsToMainMenuInsteadOfTransientStartMode()
        {
            FrontendCoordinator coordinator = CreateCoordinator(new FakeSaveRepository(), new EntryPointResolver(), out _);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            coordinator.OpenStartMode();
            Assert.That(coordinator.OpenNewGameSlots().Accepted, Is.True);

            Assert.That(coordinator.Back(), Is.True);
            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.MainMenu));
        }

        [Test]
        public void PrepareForMainMenu_ResetsBusyAndRefreshesSlots()
        {
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId);
            FakeSaveRepository repository = new FakeSaveRepository
            {
                Summaries = new[]
                {
                    new SaveSlotSummary(slotId, SaveSlotState.Valid, "Existing", DifficultyId.NormalValue, DateTime.UtcNow, false, null),
                },
            };
            FrontendCoordinator coordinator = CreateCoordinator(repository, new EntryPointResolver(), out _);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            coordinator.OpenExitConfirmation();
            coordinator.ConfirmExit(true);
            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.Busy));

            coordinator.PrepareForEntry(FrontendEntryMode.MainMenu);

            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.MainMenu));
            Assert.That(coordinator.ErrorCode, Is.Null);
            Assert.That(coordinator.SelectedSlotId, Is.Null);
            Assert.That(coordinator.Slots.Count, Is.EqualTo(1));
        }

        [Test]
        public void PrepareForOpening_ResetsTransientSelectionToLogoNotice()
        {
            FrontendCoordinator coordinator = CreateCoordinator(new FakeSaveRepository(), new EntryPointResolver(), out _);

            coordinator.CompleteLogoNotice();
            coordinator.CompleteTitleIntro();
            coordinator.OpenStartMode();
            coordinator.OpenNewGameSlots();
            SaveSlotId.TryCreate(SaveSlotId.Slot01Value, out SaveSlotId slotId);
            coordinator.SelectSlot(slotId);

            coordinator.PrepareForEntry(FrontendEntryMode.Opening);

            Assert.That(coordinator.Screen, Is.EqualTo(FrontendScreen.LogoNotice));
            Assert.That(coordinator.SelectedSlotId, Is.Null);
            Assert.That(coordinator.ErrorCode, Is.Null);
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

        private static GameSave CreateSave(SaveSlotId slotId, DateTime nowUtc)
        {
            NewGameSettings.TryCreate("Tester", DifficultyId.NormalValue, TutorialMode.DetailValue, out NewGameSettings settings, out string errorCode);
            Assert.That(settings, Is.Not.Null, errorCode);
            return GameSave.CreateNew(slotId, settings, "0.1.0", nowUtc);
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc);
        }

        private sealed class FakeSaveRepository : ISaveRepository
        {
            public IReadOnlyList<SaveSlotSummary> Summaries { get; set; } = CreateEmptySummaries();

            public SaveReadResult LoadResult { get; set; }

            public Dictionary<string, SaveReadResult> LoadResults { get; } = new Dictionary<string, SaveReadResult>();

            public SaveSlotId LastLoadedSlot { get; private set; }

            public GameSave SavedGame { get; private set; }

            public IReadOnlyList<SaveSlotSummary> ListSlots()
            {
                return Summaries;
            }

            public SaveReadResult Load(SaveSlotId slotId)
            {
                LastLoadedSlot = slotId;
                if (LoadResults.TryGetValue(slotId.Value, out SaveReadResult result))
                {
                    return result;
                }

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
