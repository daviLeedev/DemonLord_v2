using System;
using System.Collections.Generic;

namespace DemonLord.Domain
{
    public sealed class SaveSlotId : IEquatable<SaveSlotId>
    {
        public const string Slot01Value = "slot-01";
        public const string Slot02Value = "slot-02";
        public const string Slot03Value = "slot-03";

        private static readonly HashSet<string> ValidValues = new HashSet<string>(StringComparer.Ordinal)
        {
            Slot01Value,
            Slot02Value,
            Slot03Value,
        };

        private SaveSlotId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static IReadOnlyList<string> AllValues { get; } = new[]
        {
            Slot01Value,
            Slot02Value,
            Slot03Value,
        };

        public static bool TryCreate(string value, out SaveSlotId saveSlotId)
        {
            if (value != null && ValidValues.Contains(value))
            {
                saveSlotId = new SaveSlotId(value);
                return true;
            }

            saveSlotId = null;
            return false;
        }

        public override string ToString()
        {
            return Value;
        }

        public bool Equals(SaveSlotId other)
        {
            return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SaveSlotId);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }
    }
}
