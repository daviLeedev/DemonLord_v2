using System;
using System.Collections.Generic;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AreaRoot : MonoBehaviour
    {
        [SerializeField] private AreaDefinition definition;
        [SerializeField] private AreaSpawnPoint[] spawnPoints = Array.Empty<AreaSpawnPoint>();
        [SerializeField] private AreaPortal[] portals = Array.Empty<AreaPortal>();
        [SerializeField] private LocationVolume[] locationVolumes = Array.Empty<LocationVolume>();
        [SerializeField] private MapFloorVolume[] floorVolumes = Array.Empty<MapFloorVolume>();
        [SerializeField] private CameraZone[] cameraZones = Array.Empty<CameraZone>();
        [SerializeField] private PrototypeInteractable[] dialogueInteractables = Array.Empty<PrototypeInteractable>();
        [SerializeField] private LabDoorController[] doors = Array.Empty<LabDoorController>();

        public AreaDefinition Definition => definition;
        public IReadOnlyList<AreaSpawnPoint> SpawnPoints => spawnPoints ?? Array.Empty<AreaSpawnPoint>();
        public IReadOnlyList<AreaPortal> Portals => portals ?? Array.Empty<AreaPortal>();

        public void Configure(
            AreaDefinition configuredDefinition,
            AreaSpawnPoint[] configuredSpawnPoints,
            AreaPortal[] configuredPortals,
            LocationVolume[] configuredLocationVolumes,
            MapFloorVolume[] configuredFloorVolumes,
            CameraZone[] configuredCameraZones,
            PrototypeInteractable[] configuredDialogueInteractables,
            LabDoorController[] configuredDoors)
        {
            definition = configuredDefinition;
            spawnPoints = Clone(configuredSpawnPoints);
            portals = Clone(configuredPortals);
            locationVolumes = Clone(configuredLocationVolumes);
            floorVolumes = Clone(configuredFloorVolumes);
            cameraZones = Clone(configuredCameraZones);
            dialogueInteractables = Clone(configuredDialogueInteractables);
            doors = Clone(configuredDoors);
        }

        public bool TryValidate(string expectedAreaId, out string errorCode)
        {
            if (definition == null)
            {
                errorCode = "area_definition_missing";
                return false;
            }

            if (!definition.TryValidate(out errorCode))
            {
                return false;
            }

            if (!string.Equals(definition.AreaId, expectedAreaId, StringComparison.Ordinal))
            {
                errorCode = "area_root_id_mismatch";
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                errorCode = "area_spawn_points_missing";
                return false;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (AreaSpawnPoint spawn in spawnPoints)
            {
                if (spawn == null || !StableWorldId.IsValid(spawn.SpawnId) || !StableWorldId.IsValid(spawn.FloorId))
                {
                    errorCode = "area_spawn_point_invalid";
                    return false;
                }

                if (!ids.Add(spawn.SpawnId))
                {
                    errorCode = "area_spawn_id_duplicate";
                    return false;
                }
            }

            if (!ids.Contains(definition.DefaultSpawnId))
            {
                errorCode = "area_default_spawn_missing";
                return false;
            }

            errorCode = null;
            return true;
        }

        public bool TryGetSpawn(string spawnId, out AreaSpawnPoint spawn)
        {
            spawn = null;
            if (!StableWorldId.IsValid(spawnId) || spawnPoints == null)
            {
                return false;
            }

            foreach (AreaSpawnPoint candidate in spawnPoints)
            {
                if (candidate != null && string.Equals(candidate.SpawnId, spawnId, StringComparison.Ordinal))
                {
                    spawn = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetInteractable(string stableId, out PrototypeInteractable interactable)
        {
            interactable = null;
            foreach (PrototypeInteractable candidate in dialogueInteractables ?? Array.Empty<PrototypeInteractable>())
            {
                if (candidate != null && string.Equals(candidate.StableId, stableId, StringComparison.Ordinal))
                {
                    interactable = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetDoor(string stableId, out LabDoorController door)
        {
            door = null;
            foreach (LabDoorController candidate in doors ?? Array.Empty<LabDoorController>())
            {
                if (candidate != null && string.Equals(candidate.StableId, stableId, StringComparison.Ordinal))
                {
                    door = candidate;
                    return true;
                }
            }

            return false;
        }

        public void BindRuntime(
            AreaTransitionCoordinator transitionCoordinator,
            LocationTracker locationTracker,
            ExplorationLocationState locationState,
            Transform playerRoot,
            QuarterViewCameraRig cameraRig,
            DialogueFocusController dialogueController,
            NotificationView notificationView)
        {
            foreach (AreaPortal portal in portals ?? Array.Empty<AreaPortal>()) portal?.BindRuntime(transitionCoordinator, notificationView);
            foreach (LocationVolume volume in locationVolumes ?? Array.Empty<LocationVolume>()) volume?.BindTracker(locationTracker);
            foreach (MapFloorVolume volume in floorVolumes ?? Array.Empty<MapFloorVolume>()) volume?.BindRuntime(playerRoot, locationState);
            foreach (CameraZone zone in cameraZones ?? Array.Empty<CameraZone>()) zone?.BindCameraRig(cameraRig);
            foreach (PrototypeInteractable interactable in dialogueInteractables ?? Array.Empty<PrototypeInteractable>()) interactable?.BindDialogueController(dialogueController);
            foreach (LabDoorController door in doors ?? Array.Empty<LabDoorController>()) door?.BindNotificationView(notificationView);
        }

        private static T[] Clone<T>(T[] values)
        {
            return values == null ? Array.Empty<T>() : (T[])values.Clone();
        }
    }
}
