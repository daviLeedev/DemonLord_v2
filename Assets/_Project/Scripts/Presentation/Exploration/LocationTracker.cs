using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class LocationTracker : MonoBehaviour
    {
        private readonly HashSet<LocationVolume> activeVolumes = new HashSet<LocationVolume>();

        [SerializeField] private Transform playerRoot;
        private InGameHudStateSource stateSource;
        private string currentAreaId = string.Empty;
        private string currentAreaName = string.Empty;
        private string currentRoomId = string.Empty;
        private string currentRoomName = string.Empty;
        private string currentFloorId = string.Empty;

        public event Action LocationChanged;

        public string CurrentAreaId => currentAreaId;
        public string CurrentAreaName => currentAreaName;
        public string CurrentRoomId => currentRoomId;
        public string CurrentRoomName => currentRoomName;
        public string CurrentFloorId => currentFloorId;

        public void Configure(Transform configuredPlayerRoot)
        {
            playerRoot = configuredPlayerRoot;
        }

        public void Initialize(InGameHudStateSource configuredStateSource)
        {
            stateSource = configuredStateSource ?? throw new ArgumentNullException(nameof(configuredStateSource));
            RefreshLocation();
        }

        public void BeginArea(
            string areaId,
            string areaName,
            string roomId,
            string roomName,
            string floorId)
        {
            activeVolumes.Clear();
            Publish(areaId, areaName, roomId, roomName, floorId);
        }

        public void NotifyEntered(LocationVolume volume, Collider other)
        {
            if (volume == null || !IsPlayerCollider(other))
            {
                return;
            }

            if (activeVolumes.Add(volume))
            {
                RefreshLocation();
            }
        }

        public void NotifyExited(LocationVolume volume, Collider other)
        {
            if (volume == null || !IsPlayerCollider(other))
            {
                return;
            }

            if (activeVolumes.Remove(volume))
            {
                RefreshLocation();
            }
        }

        private bool IsPlayerCollider(Collider other)
        {
            return other != null
                && playerRoot != null
                && (other.transform == playerRoot || other.transform.IsChildOf(playerRoot));
        }

        private void RefreshLocation()
        {
            if (stateSource == null)
            {
                return;
            }

            LocationVolume selected = null;
            foreach (LocationVolume candidate in activeVolumes)
            {
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                if (selected == null
                    || IsCandidatePreferred(candidate.Priority, candidate.StableId, selected.Priority, selected.StableId))
                {
                    selected = candidate;
                }
            }

            // Keep the previous valid location when leaving a trigger before entering the next one.
            if (selected != null)
            {
                Publish(
                    currentAreaId,
                    selected.AreaName,
                    selected.RoomId,
                    selected.RoomName,
                    selected.FloorId);
            }
        }

        private void Publish(
            string areaId,
            string areaName,
            string roomId,
            string roomName,
            string floorId)
        {
            string nextAreaId = areaId ?? string.Empty;
            string nextAreaName = areaName ?? string.Empty;
            string nextRoomId = roomId ?? string.Empty;
            string nextRoomName = roomName ?? string.Empty;
            string nextFloorId = floorId ?? string.Empty;
            bool changed = !string.Equals(currentAreaId, nextAreaId, StringComparison.Ordinal)
                || !string.Equals(currentAreaName, nextAreaName, StringComparison.Ordinal)
                || !string.Equals(currentRoomId, nextRoomId, StringComparison.Ordinal)
                || !string.Equals(currentRoomName, nextRoomName, StringComparison.Ordinal)
                || !string.Equals(currentFloorId, nextFloorId, StringComparison.Ordinal);

            currentAreaId = nextAreaId;
            currentAreaName = nextAreaName;
            currentRoomId = nextRoomId;
            currentRoomName = nextRoomName;
            currentFloorId = nextFloorId;
            stateSource?.SetLocation(
                currentAreaId,
                currentAreaName,
                currentRoomId,
                currentRoomName,
                currentFloorId);
            if (changed)
            {
                LocationChanged?.Invoke();
            }
        }

        public static bool IsCandidatePreferred(
            int candidatePriority,
            string candidateStableId,
            int currentPriority,
            string currentStableId)
        {
            return candidatePriority > currentPriority
                || (candidatePriority == currentPriority
                    && string.CompareOrdinal(candidateStableId, currentStableId) < 0);
        }
    }
}
