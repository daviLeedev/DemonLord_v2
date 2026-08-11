using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AreaMapView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private RawImage mapImage;
        [SerializeField] private RawImage navigationOverlayImage;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform objectiveMarker;
        [SerializeField] private Text objectiveMarkerLabel;
        [SerializeField] private RectTransform[] portalMarkers = Array.Empty<RectTransform>();
        [SerializeField] private Text areaLabel;
        [SerializeField] private Text roomLabel;
        [SerializeField] private Text floorLabel;
        [SerializeField] private Text actualFloorLabel;
        [SerializeField] private Text helpLabel;

        public bool IsVisible => rootGroup != null && rootGroup.alpha > 0.5f;

        public void Configure(
            CanvasGroup group,
            RawImage image,
            RawImage overlayImage,
            RectTransform marker,
            RectTransform configuredObjectiveMarker,
            Text configuredObjectiveMarkerLabel,
            RectTransform[] configuredPortalMarkers,
            Text area,
            Text room,
            Text floor,
            Text actualFloor,
            Text help)
        {
            rootGroup = group;
            mapImage = image;
            navigationOverlayImage = overlayImage;
            playerMarker = marker;
            objectiveMarker = configuredObjectiveMarker;
            objectiveMarkerLabel = configuredObjectiveMarkerLabel;
            portalMarkers = configuredPortalMarkers == null ? Array.Empty<RectTransform>() : (RectTransform[])configuredPortalMarkers.Clone();
            areaLabel = area;
            roomLabel = room;
            floorLabel = floor;
            actualFloorLabel = actualFloor;
            helpLabel = help;
            Hide();
        }

        public void Show()
        {
            if (rootGroup == null) return;
            rootGroup.alpha = 1f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            SetMarkerVisible(playerMarker, false);
            SetMarkerVisible(objectiveMarker, false);
            HideUnusedPortalMarkers(0);
        }

        public void Render(
            AreaDefinition area,
            string roomName,
            MapFloorDefinition selectedFloor,
            string actualFloorId,
            Transform player,
            PlayerFacing facing,
            float zoom,
            IReadOnlyList<AreaPortal> portals,
            Transform objectiveTarget,
            string objectiveTitle)
        {
            if (area == null || selectedFloor == null || selectedFloor.BackgroundSprite == null || mapImage == null)
            {
                Hide();
                return;
            }

            Show();
            if (areaLabel != null) areaLabel.text = area.FallbackDisplayName;
            if (roomLabel != null) roomLabel.text = string.IsNullOrWhiteSpace(roomName) ? "현재 구역" : roomName;
            if (floorLabel != null) floorLabel.text = selectedFloor.DisplayName;
            bool actual = string.Equals(selectedFloor.FloorId, actualFloorId, StringComparison.Ordinal);
            if (actualFloorLabel != null) actualFloorLabel.text = actual ? string.Empty : "현재 위치: " + actualFloorId;
            if (helpLabel != null) helpLabel.text = "M / ESC / B 닫기    휠 확대·축소    Q / E 층 변경";

            Sprite sprite = selectedFloor.BackgroundSprite;
            mapImage.texture = sprite.texture;
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 playerNormalized = center;
            if (actual && player != null && MapProjection.TryWorldToNormalized(player.position, selectedFloor, out Vector2 projected))
            {
                playerNormalized = projected;
                center = new Vector2(Mathf.Clamp01(projected.x), Mathf.Clamp01(projected.y));
            }

            float clampedZoom = Mathf.Clamp(zoom, 1f, 2.5f);
            Vector2 uvSize = Vector2.one / clampedZoom;
            Rect localUv = new Rect(
                Mathf.Clamp(center.x - uvSize.x * 0.5f, 0f, 1f - uvSize.x),
                Mathf.Clamp(center.y - uvSize.y * 0.5f, 0f, 1f - uvSize.y),
                uvSize.x,
                uvSize.y);
            mapImage.uvRect = MiniMapView.ToTextureUv(sprite, localUv);
            RenderNavigationOverlay(selectedFloor.NavigationOverlaySprite, localUv);

            if (actual && player != null)
            {
                SetMarkerVisible(playerMarker, true);
                playerMarker.anchoredPosition = MapProjection.CalculateMiniMapMarkerPosition(playerNormalized, localUv, mapImage.rectTransform.rect.size);
                playerMarker.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    facing == null
                        ? 0f
                        : MapProjection.CalculateMarkerRotationDegrees(
                            facing.CurrentYaw,
                            selectedFloor,
                            mapImage.rectTransform.rect.size));
            }
            else
            {
                SetMarkerVisible(playerMarker, false);
            }

            if (objectiveMarker != null && actual && objectiveTarget != null
                && MapProjection.TryWorldToNormalized(objectiveTarget.position, selectedFloor, out Vector2 objectiveNormalized))
            {
                SetMarkerVisible(objectiveMarker, true);
                objectiveMarker.anchoredPosition = MapProjection.CalculateMiniMapMarkerPosition(
                    objectiveNormalized,
                    localUv,
                    mapImage.rectTransform.rect.size);
                if (objectiveMarkerLabel != null) objectiveMarkerLabel.text = objectiveTitle ?? string.Empty;
            }
            else
            {
                SetMarkerVisible(objectiveMarker, false);
            }

            int visible = 0;
            if (portals != null)
            {
                for (int index = 0; index < portals.Count && visible < portalMarkers.Length; index++)
                {
                    AreaPortal portal = portals[index];
                    if (portal == null || !MapProjection.TryWorldToNormalized(portal.transform.position, selectedFloor, out Vector2 normalized)) continue;
                    RectTransform portalMarker = portalMarkers[visible++];
                    SetMarkerVisible(portalMarker, true);
                    portalMarker.anchoredPosition = MapProjection.CalculateMiniMapMarkerPosition(normalized, localUv, mapImage.rectTransform.rect.size);
                    Text portalLabel = portalMarker.GetComponentInChildren<Text>(true);
                    if (portalLabel != null) portalLabel.text = portal.DisplayName;
                }
            }

            HideUnusedPortalMarkers(visible);
        }

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
            navigationOverlayImage.uvRect = MiniMapView.ToTextureUv(overlaySprite, localUv);
        }

        private void HideUnusedPortalMarkers(int start)
        {
            for (int index = start; index < portalMarkers.Length; index++) SetMarkerVisible(portalMarkers[index], false);
        }

        private static void SetMarkerVisible(RectTransform marker, bool visible)
        {
            if (marker != null && marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
        }
    }
}
