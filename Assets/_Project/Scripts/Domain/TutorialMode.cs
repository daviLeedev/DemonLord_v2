using System;

namespace DemonLord.Domain
{
    public sealed class TutorialMode : IEquatable<TutorialMode>
    {
        public const string DetailValue = "detail";
        public const string CoreValue = "core";
        public const string OffValue = "off";

        private TutorialMode(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static TutorialMode Detail { get; } = new TutorialMode(DetailValue);

        public static TutorialMode Core { get; } = new TutorialMode(CoreValue);

        public static TutorialMode Off { get; } = new TutorialMode(OffValue);

        public static bool TryCreate(string value, out TutorialMode mode)
        {
            string normalized = value == null ? string.Empty : value.Trim().ToLowerInvariant();
            if (normalized == DetailValue)
            {
                mode = Detail;
                return true;
            }

            if (normalized == CoreValue)
            {
                mode = Core;
                return true;
            }

            if (normalized == OffValue)
            {
                mode = Off;
                return true;
            }

            mode = null;
            return false;
        }

        public bool Equals(TutorialMode other)
        {
            return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TutorialMode);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }
    }
}
