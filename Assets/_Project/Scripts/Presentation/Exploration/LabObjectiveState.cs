using System;

namespace DemonLord.Presentation.Exploration
{
    public readonly struct LabObjectiveState : IEquatable<LabObjectiveState>
    {
        public LabObjectiveState(int stage, string title, string targetStableId, bool isComplete)
        {
            Stage = stage;
            Title = title ?? string.Empty;
            TargetStableId = targetStableId ?? string.Empty;
            IsComplete = isComplete;
        }

        public int Stage { get; }
        public string Title { get; }
        public string TargetStableId { get; }
        public bool IsComplete { get; }

        public bool Equals(LabObjectiveState other)
        {
            return Stage == other.Stage
                && IsComplete == other.IsComplete
                && string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(TargetStableId, other.TargetStableId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is LabObjectiveState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Stage;
                hash = (hash * 397) ^ IsComplete.GetHashCode();
                hash = (hash * 397) ^ Title.GetHashCode();
                hash = (hash * 397) ^ TargetStableId.GetHashCode();
                return hash;
            }
        }
    }
}
