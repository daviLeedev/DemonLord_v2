using System;

namespace DemonLord.Domain
{
    public static class ExplorationAreaIds
    {
        public const string WorldAdjustmentLabInterior = "world_adjustment_lab_interior";
        public const string BureauCourtyard = "bureau_courtyard";
    }

    public static class ExplorationSpawnIds
    {
        public const string ReceptionStart = "reception_start";
        public const string CourtyardEntrance = "courtyard_entrance";
        public const string LabExit = "lab_exit";
    }

    public static class StableWorldId
    {
        public const int MaximumLength = 64;

        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumLength)
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!((character >= 'a' && character <= 'z')
                    || (character >= '0' && character <= '9')
                    || character == '_'
                    || character == '-'))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class AreaId : IEquatable<AreaId>
    {
        private AreaId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static bool TryCreate(string value, out AreaId areaId)
        {
            if (!StableWorldId.IsValid(value))
            {
                areaId = null;
                return false;
            }

            areaId = new AreaId(value);
            return true;
        }

        public bool Equals(AreaId other)
        {
            return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AreaId);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public sealed class SpawnId : IEquatable<SpawnId>
    {
        private SpawnId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static bool TryCreate(string value, out SpawnId spawnId)
        {
            if (!StableWorldId.IsValid(value))
            {
                spawnId = null;
                return false;
            }

            spawnId = new SpawnId(value);
            return true;
        }

        public bool Equals(SpawnId other)
        {
            return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SpawnId);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public sealed class ExplorationLocation : IEquatable<ExplorationLocation>
    {
        public static ExplorationLocation Initial { get; } = Create(
            ExplorationAreaIds.WorldAdjustmentLabInterior,
            ExplorationSpawnIds.ReceptionStart);

        public ExplorationLocation(AreaId areaId, SpawnId spawnId)
        {
            AreaId = areaId ?? throw new ArgumentNullException(nameof(areaId));
            SpawnId = spawnId ?? throw new ArgumentNullException(nameof(spawnId));
        }

        public AreaId AreaId { get; }

        public SpawnId SpawnId { get; }

        public static bool TryCreate(
            string areaId,
            string spawnId,
            out ExplorationLocation location,
            out string errorCode)
        {
            if (!AreaId.TryCreate(areaId, out AreaId parsedAreaId))
            {
                location = null;
                errorCode = "invalid_area_id";
                return false;
            }

            if (!SpawnId.TryCreate(spawnId, out SpawnId parsedSpawnId))
            {
                location = null;
                errorCode = "invalid_spawn_id";
                return false;
            }

            location = new ExplorationLocation(parsedAreaId, parsedSpawnId);
            errorCode = null;
            return true;
        }

        public bool Equals(ExplorationLocation other)
        {
            return other != null && AreaId.Equals(other.AreaId) && SpawnId.Equals(other.SpawnId);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ExplorationLocation);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (AreaId.GetHashCode() * 397) ^ SpawnId.GetHashCode();
            }
        }

        private static ExplorationLocation Create(string areaId, string spawnId)
        {
            if (!TryCreate(areaId, spawnId, out ExplorationLocation location, out string errorCode))
            {
                throw new InvalidOperationException("Invalid built-in exploration location: " + errorCode);
            }

            return location;
        }
    }
}
