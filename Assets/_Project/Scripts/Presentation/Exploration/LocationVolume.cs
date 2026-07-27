using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LocationVolume : MonoBehaviour
    {
        [SerializeField] private string stableId = string.Empty;
        [SerializeField] private string areaName = "세계조정국 연구실";
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private string roomName = string.Empty;
        [SerializeField] private string floorId = "floor-1";
        [SerializeField] private int priority;
        [SerializeField] private LocationTracker tracker = null;

        public string StableId => stableId;

        public string AreaName => areaName;

        public string RoomName => roomName;

        public string RoomId => string.IsNullOrWhiteSpace(roomId) ? stableId : roomId;

        public string FloorId => floorId;

        public int Priority => priority;

        public void BindTracker(LocationTracker configuredTracker)
        {
            tracker = configuredTracker;
        }

        public void Configure(
            string configuredStableId,
            string configuredAreaName,
            string configuredRoomName,
            int configuredPriority,
            LocationTracker configuredTracker)
        {
            stableId = configuredStableId ?? string.Empty;
            areaName = configuredAreaName ?? string.Empty;
            roomName = configuredRoomName ?? string.Empty;
            priority = configuredPriority;
            tracker = configuredTracker;
            BoxCollider volumeCollider = GetComponent<BoxCollider>();
            volumeCollider.isTrigger = true;
        }

        public void Configure(
            string configuredStableId,
            string configuredAreaName,
            string configuredRoomId,
            string configuredRoomName,
            string configuredFloorId,
            int configuredPriority,
            LocationTracker configuredTracker)
        {
            Configure(
                configuredStableId,
                configuredAreaName,
                configuredRoomName,
                configuredPriority,
                configuredTracker);
            roomId = configuredRoomId ?? string.Empty;
            floorId = configuredFloorId ?? string.Empty;
        }

        private void OnTriggerEnter(Collider other)
        {
            tracker?.NotifyEntered(this, other);
        }

        private void OnTriggerExit(Collider other)
        {
            tracker?.NotifyExited(this, other);
        }
    }
}
