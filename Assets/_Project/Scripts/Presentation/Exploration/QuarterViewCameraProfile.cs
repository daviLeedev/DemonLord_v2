using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [Serializable]
    public sealed class QuarterViewCameraProfile
    {
        public const float MinimumOrthographicSize = 6f;
        public const float MaximumOrthographicSize = 12f;

        [SerializeField] private float baseYawDegrees = 45f;
        [SerializeField] private float pitchDegrees = 35f;
        [SerializeField] private float orthographicSize = 8f;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 1f, 0f);
        [SerializeField, Min(0f)] private float transitionDuration = 0.25f;

        public QuarterViewCameraProfile()
        {
        }

        public QuarterViewCameraProfile(
            float baseYawDegrees,
            float pitchDegrees,
            float orthographicSize,
            Vector3 followOffset,
            float transitionDuration)
        {
            this.baseYawDegrees = baseYawDegrees;
            this.pitchDegrees = pitchDegrees;
            this.orthographicSize = orthographicSize;
            this.followOffset = followOffset;
            this.transitionDuration = transitionDuration;
        }

        public float BaseYawDegrees => Mathf.Repeat(baseYawDegrees, 360f);

        public float PitchDegrees => Mathf.Clamp(pitchDegrees, 1f, 89f);

        public float OrthographicSize => Mathf.Clamp(
            orthographicSize,
            MinimumOrthographicSize,
            MaximumOrthographicSize);

        public Vector3 FollowOffset => followOffset;

        public float TransitionDuration => Mathf.Max(0f, transitionDuration);

        public float ResolveYawDegrees(int quarterIndex)
        {
            return Mathf.Repeat(BaseYawDegrees + NormalizeQuarterIndex(quarterIndex) * 90f, 360f);
        }

        private static int NormalizeQuarterIndex(int quarterIndex)
        {
            int normalized = quarterIndex % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }
    }
}
