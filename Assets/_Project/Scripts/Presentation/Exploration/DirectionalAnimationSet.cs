using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public enum DirectionalAnimationState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Dash = 3,
    }

    [Serializable]
    public sealed class DirectionalSpriteClip
    {
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float framesPerSecond = 8f;
        [SerializeField] private bool loop = true;

        public bool HasFrames => frames != null && frames.Length > 0 && frames[0] != null;

        public Sprite GetFrame(float elapsedSeconds)
        {
            if (frames == null || frames.Length == 0)
            {
                return null;
            }

            int frameIndex = Mathf.FloorToInt(Mathf.Max(0f, elapsedSeconds) * Mathf.Max(0.01f, framesPerSecond));
            if (loop)
            {
                frameIndex %= frames.Length;
            }
            else
            {
                frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
            }

            return frames[frameIndex];
        }

        public void Configure(Sprite[] sourceFrames, float sourceFramesPerSecond, bool shouldLoop)
        {
            frames = sourceFrames == null ? Array.Empty<Sprite>() : (Sprite[])sourceFrames.Clone();
            framesPerSecond = Mathf.Max(0.01f, sourceFramesPerSecond);
            loop = shouldLoop;
        }
    }

    [CreateAssetMenu(menuName = "DemonLord/Exploration/Directional Animation Set", fileName = "DirectionalAnimationSet")]
    public sealed class DirectionalAnimationSet : ScriptableObject
    {
        private const int DirectionCount = 8;

        [SerializeField] private DirectionalSpriteClip[] idle = CreateClips();
        [SerializeField] private DirectionalSpriteClip[] walk = CreateClips();
        [SerializeField] private DirectionalSpriteClip[] run = CreateClips();
        [SerializeField] private DirectionalSpriteClip[] dash = CreateClips();

        public Sprite GetSprite(DirectionalAnimationState state, FacingDirection8 direction, float elapsedSeconds)
        {
            DirectionalSpriteClip[] clips = GetClips(state);
            int directionIndex = NormalizeDirectionIndex((int)direction);
            return clips != null && directionIndex < clips.Length && clips[directionIndex] != null
                ? clips[directionIndex].GetFrame(elapsedSeconds)
                : null;
        }

        public bool HasCompleteSet
        {
            get
            {
                foreach (DirectionalAnimationState state in Enum.GetValues(typeof(DirectionalAnimationState)))
                {
                    DirectionalSpriteClip[] clips = GetClips(state);
                    if (clips == null || clips.Length != DirectionCount)
                    {
                        return false;
                    }

                    for (int index = 0; index < clips.Length; index++)
                    {
                        if (clips[index] == null || !clips[index].HasFrames)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public void Configure(
            Sprite[][] idleFrames,
            Sprite[][] walkFrames,
            Sprite[][] runFrames,
            Sprite[][] dashFrames)
        {
            ConfigureState(idle, idleFrames, 4f, true);
            ConfigureState(walk, walkFrames, 8f, true);
            ConfigureState(run, runFrames, 12f, true);
            ConfigureState(dash, dashFrames, 16f, false);
        }

        private static DirectionalSpriteClip[] CreateClips()
        {
            DirectionalSpriteClip[] clips = new DirectionalSpriteClip[DirectionCount];
            for (int index = 0; index < clips.Length; index++)
            {
                clips[index] = new DirectionalSpriteClip();
            }

            return clips;
        }

        private void OnEnable()
        {
            idle = EnsureClips(idle);
            walk = EnsureClips(walk);
            run = EnsureClips(run);
            dash = EnsureClips(dash);
        }

        private DirectionalSpriteClip[] GetClips(DirectionalAnimationState state)
        {
            switch (state)
            {
                case DirectionalAnimationState.Walk:
                    return walk;
                case DirectionalAnimationState.Run:
                    return run;
                case DirectionalAnimationState.Dash:
                    return dash;
                default:
                    return idle;
            }
        }

        private static void ConfigureState(
            DirectionalSpriteClip[] clips,
            Sprite[][] source,
            float framesPerSecond,
            bool loop)
        {
            if (clips == null || clips.Length != DirectionCount)
            {
                return;
            }

            for (int index = 0; index < DirectionCount; index++)
            {
                clips[index] ??= new DirectionalSpriteClip();
                Sprite[] frames = source != null && index < source.Length ? source[index] : Array.Empty<Sprite>();
                clips[index].Configure(frames, framesPerSecond, loop);
            }
        }

        private static DirectionalSpriteClip[] EnsureClips(DirectionalSpriteClip[] source)
        {
            DirectionalSpriteClip[] result = source == null || source.Length != DirectionCount
                ? CreateClips()
                : source;
            for (int index = 0; index < result.Length; index++)
            {
                result[index] ??= new DirectionalSpriteClip();
            }

            return result;
        }

        private static int NormalizeDirectionIndex(int value)
        {
            int normalized = value % DirectionCount;
            return normalized < 0 ? normalized + DirectionCount : normalized;
        }
    }
}
