using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CameraZone : MonoBehaviour
    {
        [SerializeField] private QuarterViewCameraRig cameraRig;
        [SerializeField] private QuarterViewCameraProfile profile = new QuarterViewCameraProfile();
        [SerializeField] private int priority;
        [SerializeField] private string stableId = "camera-zone";

        private readonly HashSet<EntityId> activeTargetColliders = new HashSet<EntityId>();

        public QuarterViewCameraProfile Profile => profile;

        public int Priority => priority;

        public string StableId => stableId ?? string.Empty;

        public void Configure(
            QuarterViewCameraRig rig,
            QuarterViewCameraProfile cameraProfile,
            int zonePriority,
            string zoneStableId)
        {
            cameraRig = rig != null ? rig : throw new ArgumentNullException(nameof(rig));
            profile = cameraProfile ?? throw new ArgumentNullException(nameof(cameraProfile));
            if (string.IsNullOrWhiteSpace(zoneStableId))
            {
                throw new ArgumentException("A stable camera-zone ID is required.", nameof(zoneStableId));
            }

            priority = zonePriority;
            stableId = zoneStableId.Trim();
            EnsureTrigger();
        }

        public void BindCameraRig(QuarterViewCameraRig configuredCameraRig)
        {
            cameraRig = configuredCameraRig;
            EnsureTrigger();
        }

        private void Reset()
        {
            EnsureTrigger();
        }

        private void OnValidate()
        {
            EnsureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (cameraRig == null || !cameraRig.IsTargetCollider(other))
            {
                return;
            }

            if (activeTargetColliders.Add(other.GetEntityId()) && activeTargetColliders.Count == 1)
            {
                cameraRig.NotifyZoneEntered(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !activeTargetColliders.Remove(other.GetEntityId()))
            {
                return;
            }

            if (activeTargetColliders.Count == 0 && cameraRig != null)
            {
                cameraRig.NotifyZoneExited(this);
            }
        }

        private void OnDisable()
        {
            if (activeTargetColliders.Count > 0 && cameraRig != null)
            {
                cameraRig.NotifyZoneExited(this);
            }

            activeTargetColliders.Clear();
        }

        private void EnsureTrigger()
        {
            BoxCollider zoneCollider = GetComponent<BoxCollider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }
    }
}
