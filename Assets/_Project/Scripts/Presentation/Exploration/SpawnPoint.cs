using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnKey = "start";

        public string SpawnKey => spawnKey;

        public Pose Pose => new Pose(transform.position, transform.rotation);

        public void Configure(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("A spawn point requires a non-empty key.", nameof(key));
            }

            spawnKey = key.Trim();
        }

        private void OnValidate()
        {
            if (spawnKey != null)
            {
                spawnKey = spawnKey.Trim();
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.9f);
            Gizmos.DrawWireSphere(position + Vector3.up * 0.1f, 0.35f);
            Gizmos.DrawLine(position + Vector3.up * 0.1f, position + Vector3.up * 0.1f + forward);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 0.75f, "Spawn: " + spawnKey);
#endif
        }
    }
}
