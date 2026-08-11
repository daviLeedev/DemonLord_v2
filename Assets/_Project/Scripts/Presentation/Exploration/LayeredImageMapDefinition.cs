using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [CreateAssetMenu(menuName = "DemonLord/Exploration/Layered Image Map Definition")]
    public sealed class LayeredImageMapDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private Sprite baseSprite;
        [SerializeField] private Sprite foregroundSprite;
        [SerializeField] private Sprite lightingSprite;
        [SerializeField] private Vector3 referenceWorldCenter;
        [SerializeField, Min(0.01f)] private float referenceOrthographicSize = 16f;
        [SerializeField, Min(0.01f)] private float referenceAspect = 16f / 9f;
        [SerializeField] private float referenceYaw = 45f;
        [SerializeField, Range(5f, 85f)] private float referencePitch = 35f;
        [SerializeField, Min(0.01f)] private float baseDepth = 18f;
        [SerializeField] private Color baseTint = Color.white;
        [SerializeField] private Color foregroundTint = Color.white;
        [SerializeField] private Color lightingTint = Color.white;

        public string StableId => stableId;
        public Sprite BaseSprite => baseSprite;
        public Sprite ForegroundSprite => foregroundSprite;
        public Sprite LightingSprite => lightingSprite;
        public Vector3 ReferenceWorldCenter => referenceWorldCenter;
        public float ReferenceOrthographicSize => referenceOrthographicSize;
        public float ReferenceAspect => referenceAspect;
        public float ReferenceYaw => referenceYaw;
        public float ReferencePitch => referencePitch;
        public float BaseDepth => baseDepth;
        public Color BaseTint => baseTint;
        public Color ForegroundTint => foregroundTint;
        public Color LightingTint => lightingTint;

        public void Configure(
            string configuredStableId,
            Sprite configuredBaseSprite,
            Sprite configuredForegroundSprite,
            Sprite configuredLightingSprite,
            Vector3 configuredWorldCenter,
            float configuredOrthographicSize,
            float configuredAspect,
            float configuredYaw,
            float configuredPitch,
            float configuredBaseDepth)
        {
            stableId = configuredStableId ?? string.Empty;
            baseSprite = configuredBaseSprite;
            foregroundSprite = configuredForegroundSprite;
            lightingSprite = configuredLightingSprite;
            referenceWorldCenter = configuredWorldCenter;
            referenceOrthographicSize = Mathf.Max(0.01f, configuredOrthographicSize);
            referenceAspect = Mathf.Max(0.01f, configuredAspect);
            referenceYaw = configuredYaw;
            referencePitch = Mathf.Clamp(configuredPitch, 5f, 85f);
            baseDepth = Mathf.Max(0.01f, configuredBaseDepth);
            baseTint = Color.white;
            foregroundTint = Color.white;
            lightingTint = Color.white;
        }

        public bool TryValidate(out string errorCode)
        {
            if (!StableWorldId.IsValid(stableId))
            {
                errorCode = "image_map_stable_id_invalid";
                return false;
            }

            if (baseSprite == null)
            {
                errorCode = "image_map_base_sprite_missing";
                return false;
            }

            if (referenceOrthographicSize <= 0f || referenceAspect <= 0f || baseDepth <= 0f)
            {
                errorCode = "image_map_calibration_invalid";
                return false;
            }

            errorCode = null;
            return true;
        }
    }
}
