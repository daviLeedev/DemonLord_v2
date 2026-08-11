using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public static class CourtyardNavigationPresentationContract
    {
        public const int OutputWidth = 1672;
        public const int OutputHeight = 941;
        public const float ReferenceOrthographicSize = 14f;
        public const float ReferenceYaw = 45f;
        public const float ReferencePitch = 35f;

        public static readonly Vector3 ReferenceWorldCenter = Vector3.zero;
        public static readonly Vector2 MiniMapViewportWorldSize = new Vector2(12f, 9f);

        public static float ReferenceAspect => (float)OutputWidth / OutputHeight;

        public static Quaternion ReferenceCameraRotation => Quaternion.Euler(ReferencePitch, ReferenceYaw, 0f);

        public static Vector3 MapImageAxisX
        {
            get
            {
                Vector3 projected = Vector3.ProjectOnPlane(ReferenceCameraRotation * Vector3.right, Vector3.up);
                return projected.normalized;
            }
        }

        public static Vector3 MapImageAxisY
        {
            get
            {
                Vector3 projected = Vector3.ProjectOnPlane(ReferenceCameraRotation * Vector3.up, Vector3.up);
                return projected.normalized;
            }
        }

        public static Vector2 MapImageWorldSize
        {
            get
            {
                Vector3 projectedUp = Vector3.ProjectOnPlane(ReferenceCameraRotation * Vector3.up, Vector3.up);
                float verticalScale = Mathf.Max(0.0001f, projectedUp.magnitude);
                return new Vector2(
                    ReferenceOrthographicSize * 2f * ReferenceAspect,
                    ReferenceOrthographicSize * 2f / verticalScale);
            }
        }

        public static Vector3 MapImageWorldOrigin
        {
            get
            {
                Vector2 size = MapImageWorldSize;
                return ReferenceWorldCenter
                    - MapImageAxisX * (size.x * 0.5f)
                    - MapImageAxisY * (size.y * 0.5f);
            }
        }
    }
}
