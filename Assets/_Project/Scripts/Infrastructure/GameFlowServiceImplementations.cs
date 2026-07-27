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
        public bool TryResolve(
            GameEntryPoint entryPoint,
            ExplorationLocation location,
            out EntryDestination destination,
            out string errorCode)
        {
            if (entryPoint != null
                && entryPoint.EntryId == GameEntryPoint.PrologueStartId
                && LabCheckpointId.IsKnown(entryPoint.CheckpointId)
                && location != null
                && IsKnownLocation(location))
            {
                destination = new EntryDestination(
                    "90_GameShell",
                    location.AreaId.Value,
                    location.SpawnId.Value);
                errorCode = null;
                return true;
            }

            destination = null;
            errorCode = "unsupported_entry_point";
            return false;
        }

        private static bool IsKnownLocation(ExplorationLocation location)
        {
            string areaId = location.AreaId.Value;
            string spawnId = location.SpawnId.Value;
            if (string.Equals(areaId, ExplorationAreaIds.WorldAdjustmentLabInterior, StringComparison.Ordinal))
            {
                return string.Equals(spawnId, ExplorationSpawnIds.ReceptionStart, StringComparison.Ordinal)
                    || string.Equals(spawnId, ExplorationSpawnIds.CourtyardEntrance, StringComparison.Ordinal);
            }

            return string.Equals(areaId, ExplorationAreaIds.BureauCourtyard, StringComparison.Ordinal)
                && string.Equals(spawnId, ExplorationSpawnIds.LabExit, StringComparison.Ordinal);
        }
    }
}
