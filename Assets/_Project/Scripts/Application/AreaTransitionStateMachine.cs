using System;
using DemonLord.Domain;

namespace DemonLord.Application
{
    public enum AreaTransitionState
    {
        Idle = 0,
        FadingOut = 1,
        Loading = 2,
        Validating = 3,
        Positioning = 4,
        UnloadingPrevious = 5,
        FadingIn = 6,
        RollingBack = 7,
    }

    public sealed class AreaTransitionRequest
    {
        public AreaTransitionRequest(ExplorationLocation destination)
        {
            Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        }

        public ExplorationLocation Destination { get; }
    }

    public sealed class AreaTransitionResult
    {
        private AreaTransitionResult(bool isSuccess, string errorCode)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
        }

        public bool IsSuccess { get; }

        public string ErrorCode { get; }

        public static AreaTransitionResult Success()
        {
            return new AreaTransitionResult(true, null);
        }

        public static AreaTransitionResult Failure(string errorCode)
        {
            return new AreaTransitionResult(false, string.IsNullOrWhiteSpace(errorCode) ? "area_transition_failed" : errorCode);
        }
    }

    public sealed class AreaTransitionStateMachine
    {
        public AreaTransitionState State { get; private set; } = AreaTransitionState.Idle;

        public bool IsBusy => State != AreaTransitionState.Idle;

        public bool TryBegin()
        {
            if (State != AreaTransitionState.Idle)
            {
                return false;
            }

            State = AreaTransitionState.FadingOut;
            return true;
        }

        public bool TryAdvance(AreaTransitionState expectedCurrent, AreaTransitionState next)
        {
            if (State != expectedCurrent || !IsAllowedNext(expectedCurrent, next))
            {
                return false;
            }

            State = next;
            return true;
        }

        public bool TryBeginRollback()
        {
            if (State == AreaTransitionState.Idle || State == AreaTransitionState.RollingBack)
            {
                return false;
            }

            State = AreaTransitionState.RollingBack;
            return true;
        }

        public bool TryComplete()
        {
            if (State != AreaTransitionState.FadingIn && State != AreaTransitionState.RollingBack)
            {
                return false;
            }

            State = AreaTransitionState.Idle;
            return true;
        }

        public void ForceIdle()
        {
            State = AreaTransitionState.Idle;
        }

        private static bool IsAllowedNext(AreaTransitionState current, AreaTransitionState next)
        {
            switch (current)
            {
                case AreaTransitionState.FadingOut:
                    return next == AreaTransitionState.Loading;
                case AreaTransitionState.Loading:
                    return next == AreaTransitionState.Validating;
                case AreaTransitionState.Validating:
                    return next == AreaTransitionState.Positioning;
                case AreaTransitionState.Positioning:
                    return next == AreaTransitionState.UnloadingPrevious;
                case AreaTransitionState.UnloadingPrevious:
                    return next == AreaTransitionState.FadingIn;
                default:
                    return false;
            }
        }
    }
}
