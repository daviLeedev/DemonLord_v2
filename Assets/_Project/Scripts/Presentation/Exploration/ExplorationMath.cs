using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public static class ExplorationMath
    {
        public const float DirectionEpsilon = 0.0001f;

        public static Vector3 CameraRelativeMove(Vector2 input, Transform movementBasis)
        {
            if (movementBasis == null)
            {
                return Vector3.zero;
            }

            return CameraRelativeMove(input, movementBasis.forward, movementBasis.right);
        }

        public static Vector3 CameraRelativeMove(Vector2 input, Vector3 cameraForward, Vector3 cameraRight)
        {
            Vector2 normalizedInput = Vector2.ClampMagnitude(input, 1f);
            if (normalizedInput.sqrMagnitude <= DirectionEpsilon * DirectionEpsilon)
            {
                return Vector3.zero;
            }

            Vector3 planarForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            Vector3 projectedRight = Vector3.ProjectOnPlane(cameraRight, Vector3.up);

            if (planarForward.sqrMagnitude <= DirectionEpsilon * DirectionEpsilon)
            {
                planarForward = projectedRight.sqrMagnitude > DirectionEpsilon * DirectionEpsilon
                    ? Vector3.Cross(projectedRight.normalized, Vector3.up)
                    : Vector3.forward;
            }

            planarForward.Normalize();
            Vector3 planarRight = Vector3.Cross(Vector3.up, planarForward).normalized;
            if (projectedRight.sqrMagnitude > DirectionEpsilon * DirectionEpsilon
                && Vector3.Dot(planarRight, projectedRight) < 0f)
            {
                planarRight = -planarRight;
            }

            Vector3 worldDirection = planarRight * normalizedInput.x + planarForward * normalizedInput.y;
            return Vector3.ClampMagnitude(worldDirection, 1f);
        }

        public static bool TryNormalizePlanar(Vector3 direction, out Vector3 normalized)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= DirectionEpsilon * DirectionEpsilon)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = direction.normalized;
            return true;
        }

        public static FacingDirection8 QuantizeFacing(Vector3 direction)
        {
            if (!TryNormalizePlanar(direction, out Vector3 normalized))
            {
                return FacingDirection8.North;
            }

            float angle = Mathf.Repeat(
                Mathf.Atan2(normalized.x, normalized.z) * Mathf.Rad2Deg,
                360f);
            int index = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
            return (FacingDirection8)index;
        }

        public static Vector3 FacingVector(FacingDirection8 direction)
        {
            float yaw = NormalizeFacingIndex((int)direction) * 45f;
            return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        public static float FacingYaw(FacingDirection8 direction)
        {
            return NormalizeFacingIndex((int)direction) * 45f;
        }

        public static Vector3 SelectDashDirection(Vector3 currentMovement, Vector3 lastFacing)
        {
            if (TryNormalizePlanar(currentMovement, out Vector3 normalizedCurrent))
            {
                return normalizedCurrent;
            }

            return TryNormalizePlanar(lastFacing, out Vector3 normalizedFacing)
                ? normalizedFacing
                : Vector3.zero;
        }

        private static int NormalizeFacingIndex(int index)
        {
            int normalized = index % 8;
            return normalized < 0 ? normalized + 8 : normalized;
        }
    }
}
