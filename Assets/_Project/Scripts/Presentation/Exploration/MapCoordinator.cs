using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class MapCoordinator : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private PlayerFacing playerFacing;
        [SerializeField] private LocationTracker locationTracker;
        [SerializeField] private MiniMapView miniMapView;
        [SerializeField] private AreaMapView areaMapView;
        [SerializeField] private AreaTransitionCoordinator transitionCoordinator;
        private AreaDefinition currentArea;
        private AreaRoot currentAreaRoot;
        private int selectedFloorIndex;
        private float zoom = 1f;
        private bool initialized;

        public bool IsMapOpen => areaMapView != null && areaMapView.IsVisible;

        public void Configure(
            Transform configuredPlayerRoot,
            PlayerFacing configuredPlayerFacing,
            LocationTracker configuredLocationTracker,
            MiniMapView configuredMiniMapView,
            AreaMapView configuredAreaMapView,
            AreaTransitionCoordinator configuredTransitionCoordinator)
        {
            playerRoot = configuredPlayerRoot;
            playerFacing = configuredPlayerFacing;
            locationTracker = configuredLocationTracker;
            miniMapView = configuredMiniMapView;
            areaMapView = configuredAreaMapView;
            transitionCoordinator = configuredTransitionCoordinator;
        }

        public bool TryInitialize(out string errorCode)
        {
            if (playerRoot == null || playerFacing == null || locationTracker == null
                || miniMapView == null || areaMapView == null || transitionCoordinator == null)
            {
                errorCode = "map_coordinator_reference_missing";
                return false;
            }

            transitionCoordinator.AreaChanged += OnAreaChanged;
            if (transitionCoordinator.CurrentAreaRoot != null)
            {
                OnAreaChanged(transitionCoordinator.CurrentAreaRoot.Definition, transitionCoordinator.CurrentAreaRoot);
            }

            areaMapView.Hide();
            initialized = true;
            errorCode = null;
            return true;
        }

        public bool TryOpenMap()
        {
            if (!initialized || currentArea == null || currentArea.MapDefinition == null || currentArea.MapDefinition.Floors.Count == 0)
            {
                return false;
            }

            selectedFloorIndex = FindFloorIndex(transitionCoordinator.LocationState?.FloorId);
            zoom = 1f;
            RenderAreaMap();
            return areaMapView.IsVisible;
        }

        public void CloseMap() => areaMapView?.Hide();

        public void AdjustZoom(float delta)
        {
            if (!IsMapOpen || Mathf.Abs(delta) < 0.001f) return;
            zoom = Mathf.Clamp(zoom + Mathf.Sign(delta) * 0.15f, 1f, 2.5f);
            RenderAreaMap();
        }

        public void CycleFloor(int step)
        {
            if (!IsMapOpen || step == 0 || currentArea?.MapDefinition == null) return;
            int count = currentArea.MapDefinition.Floors.Count;
            if (count <= 1) return;
            selectedFloorIndex = (selectedFloorIndex + Math.Sign(step) + count) % count;
            zoom = 1f;
            RenderAreaMap();
        }

        private void Update()
        {
            if (!initialized || currentArea?.MapDefinition == null || transitionCoordinator.LocationState == null) return;
            if (!TryGetActualFloor(out MapFloorDefinition floor)
                || !MapProjection.TryWorldToNormalized(playerRoot.position, floor, out Vector2 normalized))
            {
                miniMapView.Hide();
                return;
            }

            miniMapView.Render(floor, normalized, playerFacing.CurrentYaw);
            if (IsMapOpen) RenderAreaMap();
        }

        private void OnDestroy()
        {
            if (transitionCoordinator != null) transitionCoordinator.AreaChanged -= OnAreaChanged;
        }

        private void OnDisable() => areaMapView?.Hide();

        private void OnAreaChanged(AreaDefinition definition, AreaRoot areaRoot)
        {
            currentArea = definition;
            currentAreaRoot = areaRoot;
            selectedFloorIndex = FindFloorIndex(transitionCoordinator.LocationState?.FloorId);
            if (IsMapOpen) RenderAreaMap();
        }

        private void RenderAreaMap()
        {
            if (currentArea?.MapDefinition == null || currentArea.MapDefinition.Floors.Count == 0)
            {
                areaMapView.Hide();
                return;
            }

            selectedFloorIndex = Mathf.Clamp(selectedFloorIndex, 0, currentArea.MapDefinition.Floors.Count - 1);
            areaMapView.Render(
                currentArea,
                locationTracker.CurrentRoomName,
                currentArea.MapDefinition.Floors[selectedFloorIndex],
                transitionCoordinator.LocationState?.FloorId ?? string.Empty,
                playerRoot,
                playerFacing,
                zoom,
                currentAreaRoot?.Portals);
        }

        private bool TryGetActualFloor(out MapFloorDefinition floor)
        {
            floor = null;
            return currentArea?.MapDefinition != null
                && transitionCoordinator.LocationState != null
                && currentArea.MapDefinition.TryGetFloor(transitionCoordinator.LocationState.FloorId, out floor);
        }

        private int FindFloorIndex(string floorId)
        {
            if (currentArea?.MapDefinition == null) return 0;
            for (int index = 0; index < currentArea.MapDefinition.Floors.Count; index++)
            {
                if (string.Equals(currentArea.MapDefinition.Floors[index].FloorId, floorId, StringComparison.Ordinal)) return index;
            }

            return 0;
        }
    }
}
