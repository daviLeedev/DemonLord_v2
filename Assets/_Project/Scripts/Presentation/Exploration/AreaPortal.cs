using System;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class AreaPortal : MonoBehaviour, IExplorationInteractable
    {
        [SerializeField] private string portalId = "area-portal";
        [SerializeField] private string displayName = "출입구";
        [SerializeField] private string actionLabel = "이동하기";
        [SerializeField] private string targetAreaId = string.Empty;
        [SerializeField] private string targetSpawnId = string.Empty;
        [SerializeField] private Transform focusPoint;
        [SerializeField] private GameObject selectionMarker;
        [SerializeField] private DoorAccessPolicy accessPolicy = new DoorAccessPolicy();

        private AreaTransitionCoordinator transitionCoordinator;
        private NotificationView notificationView;

        public string StableId => portalId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public string ActionLabel => actionLabel ?? string.Empty;
        public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
        public Transform RootTransform => transform;
        public bool CanInteract => isActiveAndEnabled && gameObject.activeInHierarchy && transitionCoordinator != null && !transitionCoordinator.IsBusy;
        public string TargetAreaId => targetAreaId ?? string.Empty;
        public string TargetSpawnId => targetSpawnId ?? string.Empty;

        public void Configure(
            string configuredPortalId,
            string configuredDisplayName,
            string configuredActionLabel,
            string configuredTargetAreaId,
            string configuredTargetSpawnId,
            Transform configuredFocusPoint,
            GameObject configuredSelectionMarker,
            DoorAccessRequirement requirement = DoorAccessRequirement.None,
            string deniedMessage = "접근 권한이 없습니다. 문이 잠겨 있습니다.")
        {
            if (!StableWorldId.IsValid(configuredPortalId)
                || !StableWorldId.IsValid(configuredTargetAreaId)
                || !StableWorldId.IsValid(configuredTargetSpawnId))
            {
                throw new ArgumentException("Portal and destination IDs must be valid stable IDs.");
            }

            portalId = configuredPortalId;
            displayName = configuredDisplayName ?? string.Empty;
            actionLabel = configuredActionLabel ?? string.Empty;
            targetAreaId = configuredTargetAreaId;
            targetSpawnId = configuredTargetSpawnId;
            focusPoint = configuredFocusPoint;
            selectionMarker = configuredSelectionMarker;
            accessPolicy ??= new DoorAccessPolicy();
            accessPolicy.Configure(requirement, deniedMessage);
            SetSelected(false);
        }

        public void BindRuntime(AreaTransitionCoordinator coordinator, NotificationView notification)
        {
            transitionCoordinator = coordinator;
            notificationView = notification;
        }

        private void Awake()
        {
            SetSelected(false);
        }

        private void OnDisable()
        {
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionMarker != null && selectionMarker.activeSelf != selected)
            {
                selectionMarker.SetActive(selected);
            }
        }

        public bool TryInteract(InteractionSensor sensor)
        {
            if (!CanInteract)
            {
                return false;
            }

            if (accessPolicy != null && accessPolicy.IsLocked)
            {
                notificationView?.Show(accessPolicy.DeniedMessage);
                return true;
            }

            return transitionCoordinator.RequestTransition(targetAreaId, targetSpawnId);
        }
    }

}
