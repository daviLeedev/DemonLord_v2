using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public static class MapProjection
    {
        public static bool TryWorldToNormalized(
            Vector3 worldPosition,
            MapFloorDefinition floor,
            out Vector2 normalized)
        {
            normalized = default;
            if (floor == null || !floor.TryValidate(out _))
            {
                return false;
            }

            Vector3 axisX = floor.WorldAxisX.normalized;
            Vector3 axisY = floor.WorldAxisY.normalized;
            Vector3 offset = worldPosition - floor.WorldOrigin;
            normalized = new Vector2(
                Vector3.Dot(offset, axisX) / floor.WorldSize.x,
                Vector3.Dot(offset, axisY) / floor.WorldSize.y);
            return IsFinite(normalized.x) && IsFinite(normalized.y);
        }

        public static Vector2 NormalizedToRect(Vector2 normalized, Vector2 rectSize, bool clamp)
        {
            Vector2 value = clamp
                ? new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y))
                : normalized;
            return new Vector2(
                (value.x - 0.5f) * rectSize.x,
                (value.y - 0.5f) * rectSize.y);
        }

        public static Rect CalculateMiniMapUvRect(Vector2 playerNormalized, MapFloorDefinition floor)
        {
            if (floor == null || !floor.TryValidate(out _))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float width = Mathf.Clamp01(floor.MinimapViewportWorldSize.x / floor.WorldSize.x);
            float height = Mathf.Clamp01(floor.MinimapViewportWorldSize.y / floor.WorldSize.y);
            float x = Mathf.Clamp(playerNormalized.x - width * 0.5f, 0f, 1f - width);
            float y = Mathf.Clamp(playerNormalized.y - height * 0.5f, 0f, 1f - height);
            return new Rect(x, y, width, height);
        }

        public static Vector2 CalculateMiniMapMarkerPosition(
            Vector2 playerNormalized,
            Rect uvRect,
            Vector2 rectSize)
        {
            if (uvRect.width <= 0f || uvRect.height <= 0f)
            {
                return Vector2.zero;
            }

            Vector2 within = new Vector2(
                (playerNormalized.x - uvRect.xMin) / uvRect.width,
                (playerNormalized.y - uvRect.yMin) / uvRect.height);
            return NormalizedToRect(within, rectSize, true);
        }

        public static float CalculateMarkerRotationDegrees(
            float worldFacingYaw,
            MapFloorDefinition floor,
            Vector2 mapRectSize)
        {
            if (floor == null || !floor.TryValidate(out _))
            {
                return -worldFacingYaw;
            }

            Vector3 worldDirection = Quaternion.Euler(0f, worldFacingYaw, 0f) * Vector3.forward;
            float rectWidth = Mathf.Max(0.0001f, Mathf.Abs(mapRectSize.x));
            float rectHeight = Mathf.Max(0.0001f, Mathf.Abs(mapRectSize.y));
            float mapX = Vector3.Dot(worldDirection, floor.WorldAxisX.normalized)
                / floor.WorldSize.x
                * rectWidth;
            float mapY = Vector3.Dot(worldDirection, floor.WorldAxisY.normalized)
                / floor.WorldSize.y
                * rectHeight;
            if (Mathf.Abs(mapX) <= 0.0001f && Mathf.Abs(mapY) <= 0.0001f)
            {
                return 0f;
            }

            return -Mathf.Atan2(mapX, mapY) * Mathf.Rad2Deg;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class ExplorationLocationState
    {
        private string floorId;

        public ExplorationLocationState(DemonLord.Domain.ExplorationLocation initialLocation, string initialFloorId)
        {
            Current = initialLocation ?? throw new ArgumentNullException(nameof(initialLocation));
            floorId = initialFloorId ?? string.Empty;
        }

        public event Action Changed;

        public DemonLord.Domain.ExplorationLocation Current { get; private set; }

        public string FloorId => floorId;

        public void Set(DemonLord.Domain.ExplorationLocation location, string nextFloorId)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            string normalizedFloor = nextFloorId ?? string.Empty;
            if (Current.Equals(location) && string.Equals(floorId, normalizedFloor, StringComparison.Ordinal))
            {
                return;
            }

            Current = location;
            floorId = normalizedFloor;
            Changed?.Invoke();
        }

        public void SetFloor(string nextFloorId)
        {
            Set(Current, nextFloorId);
        }
    }
}
