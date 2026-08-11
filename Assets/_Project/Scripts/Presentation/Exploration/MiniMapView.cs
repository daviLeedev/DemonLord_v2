using System;
using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class MiniMapView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private RawImage mapImage;
        [SerializeField] private RawImage navigationOverlayImage;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform objectiveMarker;
        [SerializeField] private Text floorLabel;
        private Sprite currentSprite;
        private Rect currentUvRect;
        private Vector2 currentMarkerPosition;
        private float currentMarkerYaw = float.NaN;
        private bool warnedMissingData;

        public void Configure(
            CanvasGroup group,
            RawImage image,
            RawImage overlayImage,
            RectTransform marker,
            RectTransform configuredObjectiveMarker,
            Text floor)
        {
            rootGroup = group;
            mapImage = image;
            navigationOverlayImage = overlayImage;
            playerMarker = marker;
            objectiveMarker = configuredObjectiveMarker;
            floorLabel = floor;
            SetVisible(false);
        }

        public void Render(
            MapFloorDefinition floor,
            Vector2 playerNormalized,
            float facingYaw,
            Vector2? objectiveNormalized = null)
        {
            if (floor == null || floor.BackgroundSprite == null || mapImage == null || playerMarker == null)
            {
                SetVisible(false);
                if (!warnedMissingData)
                {
                    Debug.LogWarning("Mini-map hidden because the current floor has no valid map data.", this);
                    warnedMissingData = true;
                }

                return;
            }

            warnedMissingData = false;
            SetVisible(true);
            Sprite sprite = floor.BackgroundSprite;
            if (currentSprite != sprite)
            {
                currentSprite = sprite;
                mapImage.texture = sprite.texture;
            }

            Rect localUv = MapProjection.CalculateMiniMapUvRect(playerNormalized, floor);
            Rect textureUv = ToTextureUv(sprite, localUv);
            if (currentUvRect != textureUv)
            {
                currentUvRect = textureUv;
                mapImage.uvRect = textureUv;
            }

            RenderNavigationOverlay(floor.NavigationOverlaySprite, localUv);

            Vector2 markerPosition = MapProjection.CalculateMiniMapMarkerPosition(
                playerNormalized,
                localUv,
                mapImage.rectTransform.rect.size);
            if ((currentMarkerPosition - markerPosition).sqrMagnitude > 0.000001f)
            {
                currentMarkerPosition = markerPosition;
                playerMarker.anchoredPosition = markerPosition;
            }

            bool showObjective = objectiveMarker != null && objectiveNormalized.HasValue;
            if (objectiveMarker != null && objectiveMarker.gameObject.activeSelf != showObjective)
            {
                objectiveMarker.gameObject.SetActive(showObjective);
            }

            if (showObjective)
            {
                objectiveMarker.anchoredPosition = MapProjection.CalculateMiniMapMarkerPosition(
                    objectiveNormalized.Value,
                    localUv,
                    mapImage.rectTransform.rect.size);
            }

            if (float.IsNaN(currentMarkerYaw) || Mathf.Abs(Mathf.DeltaAngle(currentMarkerYaw, facingYaw)) > 0.01f)
            {
                currentMarkerYaw = facingYaw;
                playerMarker.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    MapProjection.CalculateMarkerRotationDegrees(
                        facingYaw,
                        floor,
                        mapImage.rectTransform.rect.size));
            }

            if (floorLabel != null && !string.Equals(floorLabel.text, floor.DisplayName, StringComparison.Ordinal))
            {
                floorLabel.text = floor.DisplayName;
            }
        }

        public void Hide() => SetVisible(false);

        private void RenderNavigationOverlay(Sprite overlaySprite, Rect localUv)
        {
            if (navigationOverlayImage == null) return;
            bool visible = overlaySprite != null && overlaySprite.texture != null;
            if (navigationOverlayImage.gameObject.activeSelf != visible)
            {
                navigationOverlayImage.gameObject.SetActive(visible);
            }

            if (!visible) return;
            navigationOverlayImage.texture = overlaySprite.texture;
            navigationOverlayImage.uvRect = ToTextureUv(overlaySprite, localUv);
        }

        private void SetVisible(bool visible)
        {
            if (rootGroup == null) return;
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        internal static Rect ToTextureUv(Sprite sprite, Rect localUv)
        {
            if (sprite == null || sprite.texture == null) return localUv;
            Rect spriteRect = sprite.textureRect;
            float textureWidth = Mathf.Max(1f, sprite.texture.width);
            float textureHeight = Mathf.Max(1f, sprite.texture.height);
            Rect baseUv = new Rect(
                spriteRect.x / textureWidth,
                spriteRect.y / textureHeight,
                spriteRect.width / textureWidth,
                spriteRect.height / textureHeight);
            return new Rect(
                baseUv.x + localUv.x * baseUv.width,
                baseUv.y + localUv.y * baseUv.height,
                localUv.width * baseUv.width,
                localUv.height * baseUv.height);
        }
    }
}
