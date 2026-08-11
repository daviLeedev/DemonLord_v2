using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class LayeredImageMapRenderer : MonoBehaviour
    {
        private const float ForegroundDepth = -3f;
        private const float LightingDepthOffset = -0.15f;

        [SerializeField] private LayeredImageMapDefinition definition;
        [SerializeField] private SpriteRenderer baseLayer;
        [SerializeField] private SpriteRenderer foregroundLayer;
        [SerializeField] private SpriteRenderer lightingLayer;
        [SerializeField] private Renderer[] hiddenEnvironmentRenderers = Array.Empty<Renderer>();

        private Camera boundCamera;

        public LayeredImageMapDefinition Definition => definition;

        public void Configure(
            LayeredImageMapDefinition configuredDefinition,
            SpriteRenderer configuredBaseLayer,
            SpriteRenderer configuredForegroundLayer,
            SpriteRenderer configuredLightingLayer,
            Renderer[] configuredHiddenRenderers)
        {
            definition = configuredDefinition;
            baseLayer = configuredBaseLayer;
            foregroundLayer = configuredForegroundLayer;
            lightingLayer = configuredLightingLayer;
            hiddenEnvironmentRenderers = configuredHiddenRenderers == null
                ? Array.Empty<Renderer>()
                : (Renderer[])configuredHiddenRenderers.Clone();
            ApplyHiddenEnvironmentState();
            ApplyPresentation();
        }

        public void BindCamera(Camera configuredCamera)
        {
            if (configuredCamera == null)
            {
                throw new ArgumentNullException(nameof(configuredCamera));
            }

            if (!TryValidate(out string errorCode))
            {
                throw new InvalidOperationException("Layered image map is invalid: " + errorCode);
            }

            boundCamera = configuredCamera;
            ApplyHiddenEnvironmentState();
            ApplyPresentation();
            ValidateCameraProjection();
        }

        public bool TryValidate(out string errorCode)
        {
            if (definition == null)
            {
                errorCode = "image_map_definition_missing";
                return false;
            }

            if (!definition.TryValidate(out errorCode))
            {
                return false;
            }

            if (baseLayer == null)
            {
                errorCode = "image_map_base_renderer_missing";
                return false;
            }

            errorCode = null;
            return true;
        }

        private void Awake()
        {
            ApplyHiddenEnvironmentState();
            ApplyPresentation();
        }

        private void OnEnable()
        {
            ApplyHiddenEnvironmentState();
            ApplyPresentation();
        }

        private void ApplyHiddenEnvironmentState()
        {
            foreach (Renderer environmentRenderer in hiddenEnvironmentRenderers ?? Array.Empty<Renderer>())
            {
                if (environmentRenderer != null)
                {
                    environmentRenderer.enabled = false;
                }
            }
        }

        private void ApplyPresentation()
        {
            if (definition == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(definition.ReferencePitch, definition.ReferenceYaw, 0f);
            Vector3 cameraForward = rotation * Vector3.forward;
            ApplyLayer(
                baseLayer,
                definition.BaseSprite,
                definition.BaseTint,
                definition.ReferenceWorldCenter + cameraForward * definition.BaseDepth,
                rotation,
                -100);
            ApplyLayer(
                foregroundLayer,
                definition.ForegroundSprite,
                definition.ForegroundTint,
                definition.ReferenceWorldCenter + cameraForward * ForegroundDepth,
                rotation,
                100);
            ApplyLayer(
                lightingLayer,
                definition.LightingSprite,
                definition.LightingTint,
                definition.ReferenceWorldCenter + cameraForward * (ForegroundDepth + LightingDepthOffset),
                rotation,
                110);
        }

        private void ApplyLayer(
            SpriteRenderer layer,
            Sprite sprite,
            Color tint,
            Vector3 position,
            Quaternion rotation,
            int sortingOrder)
        {
            if (layer == null)
            {
                return;
            }

            layer.sprite = sprite;
            layer.color = tint;
            layer.sortingOrder = sortingOrder;
            layer.shadowCastingMode = ShadowCastingMode.Off;
            layer.receiveShadows = false;
            layer.lightProbeUsage = LightProbeUsage.Off;
            layer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            layer.enabled = sprite != null;
            if (sprite == null)
            {
                return;
            }

            Transform layerTransform = layer.transform;
            layerTransform.SetPositionAndRotation(position, rotation);
            float spriteHeight = Mathf.Max(0.0001f, sprite.bounds.size.y);
            float targetHeight = definition.ReferenceOrthographicSize * 2f;
            float uniformScale = targetHeight / spriteHeight;
            layerTransform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        private void ValidateCameraProjection()
        {
            if (boundCamera == null || definition == null)
            {
                return;
            }

            Vector3 cameraEuler = boundCamera.transform.rotation.eulerAngles;
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(cameraEuler.y, definition.ReferenceYaw));
            float pitchDelta = Mathf.Abs(Mathf.DeltaAngle(cameraEuler.x, definition.ReferencePitch));
            if (!boundCamera.orthographic || yawDelta > 0.5f || pitchDelta > 0.5f)
            {
                Debug.LogWarning(
                    $"Image map '{definition.StableId}' expects orthographic yaw/pitch " +
                    $"{definition.ReferenceYaw:0.#}/{definition.ReferencePitch:0.#}, but the bound camera is " +
                    $"{cameraEuler.y:0.#}/{cameraEuler.x:0.#}.",
                    this);
            }
        }
    }
}
