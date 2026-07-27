using System;
using System.Collections.Generic;
using DemonLord.Domain;

namespace DemonLord.Application
{
    /// <summary>
    /// Declares how the frontend scene should be initialized.
    /// Opening is used only for a cold application boot; MainMenu is used when gameplay returns to title.
    /// </summary>
    public enum FrontendEntryMode
    {
        Opening,
        MainMenu,
    }

    public enum FrontendScreen
    {
        LogoNotice,
        TitleIntro,
        MainMenu,
        StartMode,
        SaveSlotsLoad,
        SaveSlotsNew,
        NewGameSetup,
        ConfirmOverwrite,
        Settings,
        ArchiveLocked,
        ConfirmExit,
        Busy,
        ErrorDialog,
    }

    public sealed class FrontendCommandResult
    {
        private FrontendCommandResult(bool accepted, EntryDestination destination, string errorCode)
        {
            Accepted = accepted;
            Destination = destination;
            ErrorCode = errorCode;
        }

        public bool Accepted { get; }

        public EntryDestination Destination { get; }

        public string ErrorCode { get; }

        public bool HasEntryDestination => Destination != null;

        public static FrontendCommandResult AcceptedWithoutEntry()
        {
            return new FrontendCommandResult(true, null, null);
        }

        public static FrontendCommandResult EntryReady(EntryDestination destination)
        {
            return new FrontendCommandResult(true, destination, null);
        }

        public static FrontendCommandResult Rejected(string errorCode)
        {
            return new FrontendCommandResult(false, null, errorCode);
        }
    }

    public sealed class FrontendCoordinator
    {
        private readonly ListSaveSlotsUseCase listSaveSlots;
        private readonly CreateNewGameUseCase createNewGame;
        private readonly LoadGameUseCase loadGame;
        private readonly IPlayerSession playerSession;
        private readonly IEntryPointResolver entryPointResolver;
        private IReadOnlyList<SaveSlotSummary> slots = new SaveSlotSummary[0];
        private FrontendScreen returnScreen;

        public FrontendCoordinator(
            ListSaveSlotsUseCase listSaveSlots,
            CreateNewGameUseCase createNewGame,
            LoadGameUseCase loadGame,
            IPlayerSession playerSession,
            IEntryPointResolver entryPointResolver)
        {
            this.listSaveSlots = listSaveSlots ?? throw new ArgumentNullException(nameof(listSaveSlots));
            this.createNewGame = createNewGame ?? throw new ArgumentNullException(nameof(createNewGame));
            this.loadGame = loadGame ?? throw new ArgumentNullException(nameof(loadGame));
            this.playerSession = playerSession ?? throw new ArgumentNullException(nameof(playerSession));
            this.entryPointResolver = entryPointResolver ?? throw new ArgumentNullException(nameof(entryPointResolver));
            Screen = FrontendScreen.LogoNotice;
        }

        public FrontendScreen Screen { get; private set; }

        public IReadOnlyList<SaveSlotSummary> Slots => slots;

        public bool HasContinueSlot => FindLatestLoadableSlot() != null;

        public SaveSlotId SelectedSlotId { get; private set; }

        public string ErrorCode { get; private set; }

        /// <summary>
        /// Resets transient frontend state before the frontend scene is presented.
        /// A gameplay return must never inherit the previous Busy state, because that state belongs to
        /// the scene that just unloaded.
        /// </summary>
        public void PrepareForEntry(FrontendEntryMode entryMode)
        {
            SelectedSlotId = null;
            ErrorCode = null;
            returnScreen = FrontendScreen.MainMenu;
            if (entryMode == FrontendEntryMode.MainMenu)
            {
                slots = listSaveSlots.Execute();
                Screen = FrontendScreen.MainMenu;
                return;
            }

            slots = new SaveSlotSummary[0];
            Screen = FrontendScreen.LogoNotice;
        }

        public bool CompleteLogoNotice()
        {
            return Move(FrontendScreen.LogoNotice, FrontendScreen.TitleIntro);
        }

        public bool CompleteTitleIntro()
        {
            return Move(FrontendScreen.TitleIntro, FrontendScreen.MainMenu);
        }

        public bool OpenStartMode()
        {
            return Move(FrontendScreen.MainMenu, FrontendScreen.StartMode);
        }

        public bool OpenSettings()
        {
            return Move(FrontendScreen.MainMenu, FrontendScreen.Settings);
        }

        public bool RefreshMainMenuSlots()
        {
            if (Screen != FrontendScreen.MainMenu)
            {
                return false;
            }

            slots = listSaveSlots.Execute();
            return true;
        }

        public FrontendCommandResult ContinueLatest()
        {
            if (Screen != FrontendScreen.MainMenu)
            {
                return FrontendCommandResult.Rejected("invalid_continue_state");
            }

            slots = listSaveSlots.Execute();
            SaveSlotSummary latestSlot = FindLatestLoadableSlot();
            if (latestSlot == null)
            {
                return FrontendCommandResult.Rejected("continue_save_not_found");
            }

            return LoadSlot(latestSlot.SlotId, FrontendScreen.MainMenu);
        }

        public bool OpenArchiveLocked()
        {
            return Move(FrontendScreen.MainMenu, FrontendScreen.ArchiveLocked);
        }

        public bool OpenExitConfirmation()
        {
            return Move(FrontendScreen.MainMenu, FrontendScreen.ConfirmExit);
        }

        public bool ConfirmExit(bool confirmed)
        {
            if (Screen != FrontendScreen.ConfirmExit)
            {
                return false;
            }

            Screen = confirmed ? FrontendScreen.Busy : FrontendScreen.MainMenu;
            return true;
        }

        public FrontendCommandResult OpenContinueSlots()
        {
            return OpenSlots(FrontendScreen.SaveSlotsLoad);
        }

        public FrontendCommandResult OpenNewGameSlots()
        {
            return OpenSlots(FrontendScreen.SaveSlotsNew);
        }

        public FrontendCommandResult SelectSlot(SaveSlotId slotId)
        {
            if (slotId == null || (Screen != FrontendScreen.SaveSlotsLoad && Screen != FrontendScreen.SaveSlotsNew))
            {
                return FrontendCommandResult.Rejected("invalid_slot_selection_state");
            }

            SelectedSlotId = slotId;
            if (Screen == FrontendScreen.SaveSlotsLoad)
            {
                return LoadSelectedSlot();
            }

            SaveSlotSummary summary = FindSlotSummary(slotId);
            if (summary == null)
            {
                return FrontendCommandResult.Rejected("slot_summary_missing");
            }

            Screen = summary.State == SaveSlotState.Empty ? FrontendScreen.NewGameSetup : FrontendScreen.ConfirmOverwrite;
            return FrontendCommandResult.AcceptedWithoutEntry();
        }

        public bool ConfirmOverwrite(bool confirmed)
        {
            if (Screen != FrontendScreen.ConfirmOverwrite)
            {
                return false;
            }

            Screen = confirmed ? FrontendScreen.NewGameSetup : FrontendScreen.SaveSlotsNew;
            return true;
        }

        public FrontendCommandResult CreateSelectedNewGame(NewGameSettings settings, string buildVersion)
        {
            if (Screen != FrontendScreen.NewGameSetup || SelectedSlotId == null || settings == null)
            {
                return FrontendCommandResult.Rejected("invalid_new_game_state");
            }

            returnScreen = FrontendScreen.NewGameSetup;
            Screen = FrontendScreen.Busy;
            CreateNewGameResult result = createNewGame.Execute(SelectedSlotId, settings, buildVersion);
            if (!result.IsSuccess)
            {
                ShowError(result.ErrorCode, FrontendScreen.NewGameSetup);
                return FrontendCommandResult.Rejected(result.ErrorCode);
            }

            return ResolveAndSetSession(result.Save);
        }

        public bool Back()
        {
            switch (Screen)
            {
                case FrontendScreen.StartMode:
                    Screen = FrontendScreen.MainMenu;
                    return true;
                case FrontendScreen.SaveSlotsLoad:
                case FrontendScreen.SaveSlotsNew:
                    // Main menu exposes the new/load choices directly. Returning to StartMode
                    // leaves the presentation with an otherwise transient state and rendered a busy spinner.
                    Screen = FrontendScreen.MainMenu;
                    return true;
                case FrontendScreen.NewGameSetup:
                case FrontendScreen.ConfirmOverwrite:
                    Screen = FrontendScreen.SaveSlotsNew;
                    return true;
                case FrontendScreen.Settings:
                case FrontendScreen.ArchiveLocked:
                case FrontendScreen.ConfirmExit:
                    Screen = FrontendScreen.MainMenu;
                    return true;
                case FrontendScreen.ErrorDialog:
                    Screen = returnScreen;
                    ErrorCode = null;
                    return true;
                default:
                    return false;
            }
        }

        public bool HandleSceneLoadFailure(string errorCode)
        {
            if (Screen != FrontendScreen.Busy)
            {
                return false;
            }

            playerSession.Clear();
            ShowError(string.IsNullOrWhiteSpace(errorCode) ? "scene_load_failed" : errorCode, returnScreen);
            return true;
        }

        private FrontendCommandResult OpenSlots(FrontendScreen targetScreen)
        {
            if (Screen != FrontendScreen.StartMode)
            {
                return FrontendCommandResult.Rejected("invalid_start_mode_state");
            }

            slots = listSaveSlots.Execute();
            SelectedSlotId = null;
            Screen = targetScreen;
            return FrontendCommandResult.AcceptedWithoutEntry();
        }

        private FrontendCommandResult LoadSelectedSlot()
        {
            SaveSlotSummary summary = FindSlotSummary(SelectedSlotId);
            if (summary == null || !summary.CanLoad)
            {
                return FrontendCommandResult.Rejected("slot_is_not_loadable");
            }

            return LoadSlot(SelectedSlotId, FrontendScreen.SaveSlotsLoad);
        }

        private FrontendCommandResult LoadSlot(SaveSlotId slotId, FrontendScreen recoveryScreen)
        {
            SaveSlotSummary summary = FindSlotSummary(slotId);
            if (summary == null || !summary.CanLoad)
            {
                return FrontendCommandResult.Rejected("slot_is_not_loadable");
            }

            SelectedSlotId = slotId;
            returnScreen = recoveryScreen;
            Screen = FrontendScreen.Busy;
            SaveReadResult result = loadGame.Execute(slotId);
            if (!result.IsSuccess)
            {
                ShowError(result.ErrorCode, recoveryScreen);
                return FrontendCommandResult.Rejected(result.ErrorCode);
            }

            return ResolveAndSetSession(result.Save);
        }

        private FrontendCommandResult ResolveAndSetSession(GameSave save)
        {
            if (!entryPointResolver.TryResolve(save.Progress, save.Location, out EntryDestination destination, out string errorCode))
            {
                ShowError(errorCode, returnScreen);
                return FrontendCommandResult.Rejected(errorCode);
            }

            playerSession.SetCurrentSave(save);
            return FrontendCommandResult.EntryReady(destination);
        }

        private void ShowError(string errorCode, FrontendScreen recoveryScreen)
        {
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "frontend_operation_failed" : errorCode;
            returnScreen = recoveryScreen;
            Screen = FrontendScreen.ErrorDialog;
        }

        private SaveSlotSummary FindSlotSummary(SaveSlotId slotId)
        {
            foreach (SaveSlotSummary summary in slots)
            {
                if (summary.SlotId.Equals(slotId))
                {
                    return summary;
                }
            }

            return null;
        }

        private SaveSlotSummary FindLatestLoadableSlot()
        {
            SaveSlotSummary latest = null;
            foreach (SaveSlotSummary slot in slots)
            {
                if (!slot.CanLoad)
                {
                    continue;
                }

                if (latest == null
                    || slot.UpdatedAtUtc.GetValueOrDefault() > latest.UpdatedAtUtc.GetValueOrDefault()
                    || (slot.UpdatedAtUtc.GetValueOrDefault() == latest.UpdatedAtUtc.GetValueOrDefault()
                        && string.CompareOrdinal(slot.SlotId.Value, latest.SlotId.Value) < 0))
                {
                    latest = slot;
                }
            }

            return latest;
        }

        private bool Move(FrontendScreen expected, FrontendScreen target)
        {
            if (Screen != expected)
            {
                return false;
            }

            Screen = target;
            return true;
        }
    }
}
