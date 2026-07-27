using System;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class MapFloorVolume : MonoBehaviour
    {
        [SerializeField] private string floorId = "floor-1";
        private Transform playerRoot;
        private ExplorationLocationState locationState;

        public string FloorId => floorId ?? string.Empty;

        public void Configure(string configuredFloorId)
        {
            if (!StableWorldId.IsValid(configuredFloorId))
            {
                throw new ArgumentException("A valid floor ID is required.", nameof(configuredFloorId));
            }

            floorId = configuredFloorId;
            GetComponent<BoxCollider>().isTrigger = true;
        }

        public void BindRuntime(Transform configuredPlayerRoot, ExplorationLocationState configuredLocationState)
        {
            playerRoot = configuredPlayerRoot;
            locationState = configuredLocationState;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && playerRoot != null
                && (other.transform == playerRoot || other.transform.IsChildOf(playerRoot)))
            {
                locationState?.SetFloor(FloorId);
            }
        }
    }
}
