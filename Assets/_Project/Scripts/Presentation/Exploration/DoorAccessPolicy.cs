using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public enum DoorAccessRequirement
    {
        None = 0,
        AlwaysLocked = 1,
    }

    [Serializable]
    public sealed class DoorAccessPolicy
    {
        [SerializeField] private DoorAccessRequirement requirement;
        [SerializeField] private string deniedMessage = "접근 권한이 없습니다. 문이 잠겨 있습니다.";

        public bool IsLocked => requirement == DoorAccessRequirement.AlwaysLocked;

        public string DeniedMessage => string.IsNullOrWhiteSpace(deniedMessage)
            ? "접근 권한이 없습니다. 문이 잠겨 있습니다."
            : deniedMessage;

        public void Configure(DoorAccessRequirement configuredRequirement, string configuredDeniedMessage)
        {
            requirement = configuredRequirement;
            deniedMessage = string.IsNullOrWhiteSpace(configuredDeniedMessage)
                ? "접근 권한이 없습니다. 문이 잠겨 있습니다."
                : configuredDeniedMessage.Trim();
        }
    }
}
