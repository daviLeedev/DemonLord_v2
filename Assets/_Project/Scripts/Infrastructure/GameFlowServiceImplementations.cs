using System;
using DemonLord.Application;
using DemonLord.Domain;

namespace DemonLord.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public sealed class InMemoryPlayerSession : IPlayerSession
    {
        public GameSave CurrentSave { get; private set; }

        public void SetCurrentSave(GameSave save)
        {
            CurrentSave = save ?? throw new ArgumentNullException(nameof(save));
        }

        public void Clear()
        {
            CurrentSave = null;
        }
    }

    public sealed class EntryPointResolver : IEntryPointResolver
    {
        public bool TryResolve(GameEntryPoint entryPoint, out EntryDestination destination, out string errorCode)
        {
            if (entryPoint != null
                && entryPoint.EntryId == GameEntryPoint.PrologueStartId
                && entryPoint.CheckpointId == "start")
            {
                destination = new EntryDestination("90_GameShell", "start");
                errorCode = null;
                return true;
            }

            destination = null;
            errorCode = "unsupported_entry_point";
            return false;
        }
    }
}
