using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public readonly struct InteractionCandidateScore
    {
        public InteractionCandidateScore(string stableId, float alignment, float distanceSquared)
        {
            StableId = stableId ?? string.Empty;
            Alignment = alignment;
            DistanceSquared = distanceSquared;
        }

        public string StableId { get; }

        public float Alignment { get; }

        public float DistanceSquared { get; }
    }

    public static class InteractionSelection
    {
        private const float ComparisonEpsilon = 0.0001f;

        public static bool TryCreateScore(
            Vector3 origin,
            Vector3 facing,
            Vector3 target,
            float radius,
            float fullConeAngle,
            string stableId,
            out InteractionCandidateScore score)
        {
            score = default;
            if (string.IsNullOrWhiteSpace(stableId) || radius <= 0f)
            {
                return false;
            }

            Vector3 offset = target - origin;
            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared > radius * radius)
            {
                return false;
            }

            facing.y = 0f;
            if (facing.sqrMagnitude <= ComparisonEpsilon)
            {
                facing = Vector3.forward;
            }

            float alignment = distanceSquared <= ComparisonEpsilon
                ? 1f
                : Vector3.Dot(facing.normalized, offset.normalized);
            float halfCone = Mathf.Clamp(fullConeAngle * 0.5f, 0f, 180f);
            float minimumAlignment = Mathf.Cos(halfCone * Mathf.Deg2Rad);
            if (alignment + ComparisonEpsilon < minimumAlignment)
            {
                return false;
            }

            score = new InteractionCandidateScore(stableId.Trim(), alignment, distanceSquared);
            return true;
        }

        public static bool IsBetter(
            InteractionCandidateScore candidate,
            InteractionCandidateScore incumbent)
        {
            float alignmentDifference = candidate.Alignment - incumbent.Alignment;
            if (Mathf.Abs(alignmentDifference) > ComparisonEpsilon)
            {
                return alignmentDifference > 0f;
            }

            float distanceDifference = candidate.DistanceSquared - incumbent.DistanceSquared;
            if (Mathf.Abs(distanceDifference) > ComparisonEpsilon)
            {
                return distanceDifference < 0f;
            }

            return string.Compare(candidate.StableId, incumbent.StableId, StringComparison.Ordinal) < 0;
        }
    }
}
