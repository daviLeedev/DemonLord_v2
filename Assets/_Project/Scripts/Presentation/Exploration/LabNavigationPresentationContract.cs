using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    /// <summary>
    /// Shared calibration used by the lab image map, authoring guide and map UI.
    /// Gameplay collision remains world-space; presentation assets are derived from this contract.
    /// </summary>
    public static class LabNavigationPresentationContract
    {
        public const int OutputWidth = 1672;
        public const int OutputHeight = 941;
        public const float ReferenceOrthographicSize = 18f;
        public const float ReferenceYaw = 45f;
        public const float ReferencePitch = 35f;
        public const float BaseDepth = 18f;

        public static readonly Vector3 ReferenceWorldCenter = new Vector3(0f, 0f, 2.5f);
        public static readonly Vector3 MapWorldOrigin = new Vector3(-15f, 0f, -14f);
        public static readonly Vector2 MapWorldSize = new Vector2(30f, 34f);
        public static readonly Vector2 MiniMapViewportWorldSize = new Vector2(13f, 10f);

        public static float ReferenceAspect => (float)OutputWidth / OutputHeight;

        public static Quaternion ReferenceCameraRotation =>
            Quaternion.Euler(ReferencePitch, ReferenceYaw, 0f);

        public static Vector3 MapImageAxisX
        {
            get
            {
                Vector3 projectedRight = Vector3.ProjectOnPlane(
                    ReferenceCameraRotation * Vector3.right,
                    Vector3.up);
                return projectedRight.normalized;
            }
        }

        public static Vector3 MapImageAxisY
        {
            get
            {
                Vector3 projectedUp = Vector3.ProjectOnPlane(
                    ReferenceCameraRotation * Vector3.up,
                    Vector3.up);
                return projectedUp.normalized;
            }
        }

        public static Vector2 MapImageWorldSize
        {
            get
            {
                Vector3 projectedUp = Vector3.ProjectOnPlane(
                    ReferenceCameraRotation * Vector3.up,
                    Vector3.up);
                float verticalProjectionScale = Mathf.Max(0.0001f, projectedUp.magnitude);
                return new Vector2(
                    ReferenceOrthographicSize * 2f * ReferenceAspect,
                    ReferenceOrthographicSize * 2f / verticalProjectionScale);
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
