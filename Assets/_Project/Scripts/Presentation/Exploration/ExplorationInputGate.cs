using System;
using System.Collections.Generic;

namespace DemonLord.Presentation.Exploration
{
    [Flags]
    public enum ExplorationInputChannel
    {
        None = 0,
        Movement = 1 << 0,
        Dash = 1 << 1,
        Interaction = 1 << 2,
        Camera = 1 << 3,
        Dialogue = 1 << 4,
        Menu = 1 << 5,
        Locomotion = Movement | Dash,
        All = Movement | Dash | Interaction | Camera | Dialogue,
    }

    public sealed class ExplorationInputGate
    {
        private readonly Dictionary<int, ExplorationInputChannel> locks =
            new Dictionary<int, ExplorationInputChannel>();

        private int nextTokenId = 1;

        public ExplorationInputChannel LockedChannels { get; private set; }

        public bool IsAllowed(ExplorationInputChannel channels)
        {
            return (LockedChannels & channels) == ExplorationInputChannel.None;
        }

        public bool IsBlocked(ExplorationInputChannel channels)
        {
            return !IsAllowed(channels);
        }

        public IDisposable AcquireLock(ExplorationInputChannel channels)
        {
            if (channels == ExplorationInputChannel.None)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "At least one input channel must be locked.");
            }

            int tokenId = nextTokenId++;
            if (nextTokenId <= 0)
            {
                nextTokenId = 1;
            }

            locks.Add(tokenId, channels);
            LockedChannels |= channels;
            return new LockToken(this, tokenId);
        }

        private void Release(int tokenId)
        {
            if (!locks.Remove(tokenId))
            {
                return;
            }

            ExplorationInputChannel aggregate = ExplorationInputChannel.None;
            foreach (ExplorationInputChannel channels in locks.Values)
            {
                aggregate |= channels;
            }

            LockedChannels = aggregate;
        }

        private sealed class LockToken : IDisposable
        {
            private ExplorationInputGate owner;
            private readonly int tokenId;

            public LockToken(ExplorationInputGate owner, int tokenId)
            {
                this.owner = owner;
                this.tokenId = tokenId;
            }

            public void Dispose()
            {
                ExplorationInputGate currentOwner = owner;
                if (currentOwner == null)
                {
                    return;
                }

                owner = null;
                currentOwner.Release(tokenId);
            }
        }
    }
}
