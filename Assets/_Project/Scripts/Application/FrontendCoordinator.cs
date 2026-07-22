using System;
using System.Collections.Generic;
using DemonLord.Domain;

namespace DemonLord.Application
{
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

        public SaveSlotId SelectedSlotId { get; private set; }

        public string ErrorCode { get; private set; }

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
                    Screen = FrontendScreen.StartMode;
                    return true;
                case FrontendScreen.NewGameSetup:
                case FrontendScreen.ConfirmOverwrite:
                    Screen = FrontendScreen.SaveSlotsNew;
                    return true;
                case FrontendScreen.ErrorDialog:
                    Screen = returnScreen;
                    ErrorCode = null;
                    return true;
                default:
                    return false;
            }
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

            returnScreen = FrontendScreen.SaveSlotsLoad;
            Screen = FrontendScreen.Busy;
            SaveReadResult result = loadGame.Execute(SelectedSlotId);
            if (!result.IsSuccess)
            {
                ShowError(result.ErrorCode, FrontendScreen.SaveSlotsLoad);
                return FrontendCommandResult.Rejected(result.ErrorCode);
            }

            return ResolveAndSetSession(result.Save);
        }

        private FrontendCommandResult ResolveAndSetSession(GameSave save)
        {
            if (!entryPointResolver.TryResolve(save.Progress, out EntryDestination destination, out string errorCode))
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
