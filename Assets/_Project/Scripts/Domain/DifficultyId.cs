using System;
using System.Collections.Generic;

namespace DemonLord.Domain
{
    public sealed class DifficultyId : IEquatable<DifficultyId>
    {
        public const string StoryValue = "story";
        public const string NormalValue = "normal";
        public const string HardValue = "hard";

        private static readonly HashSet<string> ValidValues = new HashSet<string>(StringComparer.Ordinal)
        {
            StoryValue,
            NormalValue,
            HardValue,
        };

        private DifficultyId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static IReadOnlyList<string> AllValues { get; } = new[]
        {
            StoryValue,
            NormalValue,
            HardValue,
        };

        public static bool TryCreate(string value, out DifficultyId difficultyId)
        {
            if (value != null && ValidValues.Contains(value))
            {
                difficultyId = new DifficultyId(value);
                return true;
            }

            difficultyId = null;
            return false;
        }

        public bool Equals(DifficultyId other)
        {
            return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DifficultyId);
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
}
