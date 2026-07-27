using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public enum FacingDirection8
    {
        North = 0,
        NorthEast = 1,
        East = 2,
        SouthEast = 3,
        South = 4,
        SouthWest = 5,
        West = 6,
        NorthWest = 7,
    }

    public readonly struct PlayerFacingState
    {
        public PlayerFacingState(
            FacingDirection8 direction,
            Vector3 worldDirection,
            bool isExactFacing)
        {
            Direction = direction;
            WorldDirection = worldDirection;
            IsExactFacing = isExactFacing;
        }

        public FacingDirection8 Direction { get; }

        public Vector3 WorldDirection { get; }

        public bool IsExactFacing { get; }
    }

    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private FacingDirection8 initialDirection = FacingDirection8.South;
        [SerializeField] private float modelForwardYawOffset = 0f;

        public FacingDirection8 CurrentDirection { get; private set; }

        public Vector3 CurrentWorldDirection { get; private set; }

        public bool IsExactFacing { get; private set; }

        public float CurrentYaw => Mathf.Repeat(
            Mathf.Atan2(CurrentWorldDirection.x, CurrentWorldDirection.z) * Mathf.Rad2Deg,
            360f);

        private void Awake()
        {
            SetFacing(initialDirection);
        }

        public void SetVisualRoot(Transform targetVisualRoot)
        {
            visualRoot = targetVisualRoot;
            ApplyVisualRotation();
        }

        public void SetFacing(FacingDirection8 direction)
        {
            CurrentDirection = direction;
            CurrentWorldDirection = ExplorationMath.FacingVector(direction);
            IsExactFacing = false;
            ApplyVisualRotation();
        }

        public bool FaceMovementDirection(Vector3 worldDirection)
        {
            if (!ExplorationMath.TryNormalizePlanar(worldDirection, out Vector3 normalized))
            {
                return false;
            }

            SetFacing(ExplorationMath.QuantizeFacing(normalized));
            return true;
        }

        public bool FaceDirection(Vector3 worldDirection)
        {
            return FaceMovementDirection(worldDirection);
        }

        public bool FaceDirectionExact(Vector3 worldDirection)
        {
            if (!ExplorationMath.TryNormalizePlanar(worldDirection, out Vector3 normalized))
            {
                return false;
            }

            CurrentDirection = ExplorationMath.QuantizeFacing(normalized);
            CurrentWorldDirection = normalized;
            IsExactFacing = true;
            ApplyVisualRotation();
            return true;
        }

        public bool FaceTargetExact(Vector3 targetWorldPosition)
        {
            return FaceDirectionExact(targetWorldPosition - transform.position);
        }

        public bool FaceTarget(Vector3 targetWorldPosition)
        {
            return FaceTargetExact(targetWorldPosition);
        }

        public PlayerFacingState CaptureState()
        {
            return new PlayerFacingState(CurrentDirection, CurrentWorldDirection, IsExactFacing);
        }

        public void RefreshVisual()
        {
            ApplyVisualRotation();
        }

        public void RestoreState(PlayerFacingState state)
        {
            CurrentDirection = state.Direction;
            CurrentWorldDirection = ExplorationMath.TryNormalizePlanar(state.WorldDirection, out Vector3 normalized)
                ? normalized
                : ExplorationMath.FacingVector(state.Direction);
            IsExactFacing = state.IsExactFacing;
            ApplyVisualRotation();
        }

        private void ApplyVisualRotation()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.rotation = Quaternion.Euler(0f, CurrentYaw + modelForwardYawOffset, 0f);
        }
    }
}
