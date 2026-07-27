using System;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AreaSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = string.Empty;
        [SerializeField] private string floorId = "floor-1";

        public string SpawnId => spawnId ?? string.Empty;
        public string FloorId => floorId ?? string.Empty;
        public Pose Pose => new Pose(transform.position, transform.rotation);

        public void Configure(string configuredSpawnId, string configuredFloorId)
        {
            if (!StableWorldId.IsValid(configuredSpawnId))
            {
                throw new ArgumentException("A valid spawn ID is required.", nameof(configuredSpawnId));
            }

            if (!StableWorldId.IsValid(configuredFloorId))
            {
                throw new ArgumentException("A valid floor ID is required.", nameof(configuredFloorId));
            }

            spawnId = configuredSpawnId;
            floorId = configuredFloorId;
        }
    }
}
