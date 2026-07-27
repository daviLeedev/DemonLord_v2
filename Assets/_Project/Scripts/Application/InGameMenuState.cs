namespace DemonLord.Application
{
    public enum InGameMenuState
    {
        Closed,
        Root,
        Settings,
        Controls,
        ConfirmReturnToTitle,
        ConfirmQuit,
        Busy,
    }

    /// <summary>
    /// Pure state model for the exploration pause menu. The presentation coordinator owns the
    /// side effects (input locks, time scale, saves and scene transitions).
    /// </summary>
    public sealed class InGameMenuStateMachine
    {
        private InGameMenuState busyReturnState;

        public InGameMenuState State { get; private set; } = InGameMenuState.Closed;

        public bool IsMenuVisible => State != InGameMenuState.Closed;

        public bool TryOpenRoot()
        {
            if (State != InGameMenuState.Closed)
            {
                return false;
            }

            State = InGameMenuState.Root;
            return true;
        }

        public bool TryOpenSettings()
        {
            return TryTransitionFromRoot(InGameMenuState.Settings);
        }

        public bool TryOpenControls()
        {
            return TryTransitionFromRoot(InGameMenuState.Controls);
        }

        public bool TryConfirmReturnToTitle()
        {
            return TryTransitionFromRoot(InGameMenuState.ConfirmReturnToTitle);
        }

        public bool TryConfirmQuit()
        {
            return TryTransitionFromRoot(InGameMenuState.ConfirmQuit);
        }

        public bool TryBeginBusy()
        {
            if (State == InGameMenuState.Closed || State == InGameMenuState.Busy)
            {
                return false;
            }

            busyReturnState = State;
            State = InGameMenuState.Busy;
            return true;
        }

        public bool TryCompleteBusy(bool succeeded)
        {
            if (State != InGameMenuState.Busy)
            {
                return false;
            }

            State = succeeded ? InGameMenuState.Root : busyReturnState;
            return true;
        }

        public InGameMenuBackResult TryBack()
        {
            switch (State)
            {
                case InGameMenuState.Root:
                    State = InGameMenuState.Closed;
                    return InGameMenuBackResult.CloseMenu;
                case InGameMenuState.Settings:
                case InGameMenuState.Controls:
                case InGameMenuState.ConfirmReturnToTitle:
                case InGameMenuState.ConfirmQuit:
                    State = InGameMenuState.Root;
                    return InGameMenuBackResult.ReturnToRoot;
                default:
                    return InGameMenuBackResult.Rejected;
            }
        }

        public bool ForceRoot()
        {
            if (State == InGameMenuState.Closed || State == InGameMenuState.Busy)
            {
                return false;
            }

            State = InGameMenuState.Root;
            return true;
        }

        private bool TryTransitionFromRoot(InGameMenuState target)
        {
            if (State != InGameMenuState.Root)
            {
                return false;
            }

            State = target;
            return true;
        }
    }

    public enum InGameMenuBackResult
    {
        Rejected,
        ReturnToRoot,
        CloseMenu,
    }
}
