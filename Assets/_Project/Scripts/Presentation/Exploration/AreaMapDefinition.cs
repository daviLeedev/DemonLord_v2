using System;
using System.Collections.Generic;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [CreateAssetMenu(menuName = "DemonLord/Exploration/Area Map Definition", fileName = "AreaMapDefinition")]
    public sealed class AreaMapDefinition : ScriptableObject
    {
        [SerializeField] private MapFloorDefinition[] floors = Array.Empty<MapFloorDefinition>();

        public IReadOnlyList<MapFloorDefinition> Floors => floors ?? Array.Empty<MapFloorDefinition>();

        public void Configure(MapFloorDefinition[] configuredFloors)
        {
            floors = configuredFloors == null
                ? Array.Empty<MapFloorDefinition>()
                : (MapFloorDefinition[])configuredFloors.Clone();
        }

        public bool TryGetFloor(string floorId, out MapFloorDefinition floor)
        {
            floor = null;
            if (!StableWorldId.IsValid(floorId) || floors == null)
            {
                return false;
            }

            foreach (MapFloorDefinition candidate in floors)
            {
                if (candidate != null && string.Equals(candidate.FloorId, floorId, StringComparison.Ordinal))
                {
                    floor = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryValidate(out string errorCode)
        {
            if (floors == null || floors.Length == 0)
            {
                errorCode = "map_floors_missing";
                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (MapFloorDefinition floor in floors)
            {
                if (floor == null)
                {
                    errorCode = "map_floor_invalid";
                    return false;
                }

                if (!floor.TryValidate(out errorCode))
                {
                    return false;
                }

                if (!ids.Add(floor.FloorId))
                {
                    errorCode = "map_floor_id_duplicate";
                    return false;
                }
            }

            errorCode = null;
            return true;
        }
    }

    [Serializable]
    public sealed class MapFloorDefinition
    {
        [SerializeField] private string floorId = "floor-1";
        [SerializeField] private string displayName = "1층";
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite navigationOverlaySprite;
        [SerializeField] private Vector3 worldOrigin;
        [SerializeField] private Vector3 worldAxisX = Vector3.right;
        [SerializeField] private Vector3 worldAxisY = Vector3.forward;
        [SerializeField] private Vector2 worldSize = new Vector2(32f, 32f);
        [SerializeField] private Vector2 minimapViewportWorldSize = new Vector2(14f, 10f);

        public string FloorId => floorId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite NavigationOverlaySprite => navigationOverlaySprite;
        public Vector3 WorldOrigin => worldOrigin;
        public Vector3 WorldAxisX => worldAxisX;
        public Vector3 WorldAxisY => worldAxisY;
        public Vector2 WorldSize => worldSize;
        public Vector2 MinimapViewportWorldSize => minimapViewportWorldSize;

        public void Configure(
            string configuredFloorId,
            string configuredDisplayName,
            Sprite configuredBackgroundSprite,
            Vector3 configuredWorldOrigin,
            Vector3 configuredWorldAxisX,
            Vector3 configuredWorldAxisY,
            Vector2 configuredWorldSize,
            Vector2 configuredMinimapViewportWorldSize,
            Sprite configuredNavigationOverlaySprite = null)
        {
            floorId = configuredFloorId ?? string.Empty;
            displayName = configuredDisplayName ?? string.Empty;
            backgroundSprite = configuredBackgroundSprite;
            navigationOverlaySprite = configuredNavigationOverlaySprite;
            worldOrigin = configuredWorldOrigin;
            worldAxisX = configuredWorldAxisX;
            worldAxisY = configuredWorldAxisY;
            worldSize = configuredWorldSize;
            minimapViewportWorldSize = configuredMinimapViewportWorldSize;
        }

        public bool TryValidate(out string errorCode)
        {
            if (!StableWorldId.IsValid(FloorId))
            {
                errorCode = "invalid_floor_id";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                errorCode = "floor_display_name_missing";
                return false;
            }

            if (worldAxisX.sqrMagnitude < 0.0001f || worldAxisY.sqrMagnitude < 0.0001f
                || Vector3.Cross(worldAxisX, worldAxisY).sqrMagnitude < 0.0001f)
            {
                errorCode = "map_floor_axes_invalid";
                return false;
            }

            if (worldSize.x <= 0f || worldSize.y <= 0f
                || minimapViewportWorldSize.x <= 0f || minimapViewportWorldSize.y <= 0f)
            {
                errorCode = "map_floor_size_invalid";
                return false;
            }

            errorCode = null;
            return true;
        }
    }
}
