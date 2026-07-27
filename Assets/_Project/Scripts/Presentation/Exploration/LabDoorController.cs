using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public enum LabDoorState
    {
        Closed = 0,
        Opening = 1,
        Open = 2,
        Closing = 3,
        Locked = 4,
    }

    [DisallowMultipleComponent]
    public sealed class LabDoorController : MonoBehaviour, IExplorationInteractable
    {
        private const int ObstructionBufferSize = 16;

        [SerializeField] private string stableId = "lab-door";
        [SerializeField] private string displayName = "연구실 문";
        [SerializeField] private Transform focusPoint;
        [SerializeField] private GameObject selectionMarker;
        [SerializeField] private Transform movingLeaf;
        [SerializeField] private Collider blockingCollider;
        [SerializeField] private BoxCollider closingObstructionVolume;
        [SerializeField] private NotificationView notificationView;
        [SerializeField, Min(0.01f)] private float transitionSeconds = 0.45f;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(1.6f, 0f, 0f);
        [SerializeField] private DoorAccessPolicy accessPolicy = new DoorAccessPolicy();

        private readonly Collider[] obstructionBuffer = new Collider[ObstructionBufferSize];
        private Vector3 closedLocalPosition;
        private Vector3 openLocalPosition;
        private LabDoorState state;
        private float transitionElapsed;

        public string StableId => stableId ?? string.Empty;

        public string DisplayName => displayName ?? string.Empty;

        public string ActionLabel
        {
            get
            {
                switch (state)
                {
                    case LabDoorState.Open:
                        return "문 닫기";
                    case LabDoorState.Locked:
                        return "조사하기";
                    default:
                        return "문 열기";
                }
            }
        }

        public Transform FocusPoint => focusPoint != null ? focusPoint : transform;

        public Transform RootTransform => transform;

        public bool CanInteract => isActiveAndEnabled && gameObject.activeInHierarchy;

        public LabDoorState State => state;

        public bool StartsLocked => accessPolicy != null && accessPolicy.IsLocked;

        public bool IsLocked => state == LabDoorState.Locked;

        private void Awake()
        {
            if (movingLeaf == null)
            {
                movingLeaf = transform;
            }

            closedLocalPosition = movingLeaf.localPosition;
            openLocalPosition = closedLocalPosition + openLocalOffset;
            state = accessPolicy != null && accessPolicy.IsLocked ? LabDoorState.Locked : LabDoorState.Closed;
            ApplyStateImmediate();
            SetSelected(false);
        }

        private void Update()
        {
            if (state != LabDoorState.Opening && state != LabDoorState.Closing)
            {
                return;
            }

            transitionElapsed = Mathf.Min(transitionSeconds, transitionElapsed + Time.deltaTime);
            float progress = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionSeconds));
            float eased = progress * progress * (3f - 2f * progress);
            bool isOpening = state == LabDoorState.Opening;
            movingLeaf.localPosition = Vector3.Lerp(
                isOpening ? closedLocalPosition : openLocalPosition,
                isOpening ? openLocalPosition : closedLocalPosition,
                eased);

            if (isOpening && progress >= 0.85f && blockingCollider != null)
            {
                blockingCollider.enabled = false;
            }

            if (progress < 1f)
            {
                return;
            }

            state = isOpening ? LabDoorState.Open : LabDoorState.Closed;
            if (!isOpening && blockingCollider != null)
            {
                blockingCollider.enabled = true;
            }
        }

        private void OnDisable()
        {
            SetSelected(false);
        }

        public void Configure(
            string configuredStableId,
            string configuredDisplayName,
            Transform configuredFocusPoint,
            GameObject configuredSelectionMarker,
            Transform configuredMovingLeaf,
            Collider configuredBlockingCollider,
            BoxCollider configuredClosingObstructionVolume,
            NotificationView configuredNotificationView,
            Vector3 configuredOpenLocalOffset,
            DoorAccessRequirement configuredRequirement,
            string configuredDeniedMessage)
        {
            if (string.IsNullOrWhiteSpace(configuredStableId))
            {
                throw new ArgumentException("A door requires a stable ID.", nameof(configuredStableId));
            }

            stableId = configuredStableId.Trim();
            displayName = configuredDisplayName ?? string.Empty;
            focusPoint = configuredFocusPoint;
            selectionMarker = configuredSelectionMarker;
            movingLeaf = configuredMovingLeaf ?? throw new ArgumentNullException(nameof(configuredMovingLeaf));
            blockingCollider = configuredBlockingCollider ?? throw new ArgumentNullException(nameof(configuredBlockingCollider));
            closingObstructionVolume = configuredClosingObstructionVolume;
            notificationView = configuredNotificationView;
            openLocalOffset = configuredOpenLocalOffset;
            accessPolicy ??= new DoorAccessPolicy();
            accessPolicy.Configure(configuredRequirement, configuredDeniedMessage);
            closedLocalPosition = movingLeaf.localPosition;
            openLocalPosition = closedLocalPosition + openLocalOffset;
            state = accessPolicy.IsLocked ? LabDoorState.Locked : LabDoorState.Closed;
            transitionElapsed = 0f;
            ApplyStateImmediate();
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

            switch (state)
            {
                case LabDoorState.Locked:
                    notificationView?.Show(accessPolicy != null ? accessPolicy.DeniedMessage : "접근 권한이 없습니다. 문이 잠겨 있습니다.");
                    return true;
                case LabDoorState.Closed:
                    BeginOpening();
                    return true;
                case LabDoorState.Open:
                    return BeginClosing();
                default:
                    return false;
            }
        }

        public void BindNotificationView(NotificationView configuredNotificationView)
        {
            notificationView = configuredNotificationView;
        }

        /// <summary>
        /// Releases a progression-gated door. The authored policy is deliberately
        /// retained so a new scene load starts locked until saved progress restores it.
        /// </summary>
        public void Unlock()
        {
            if (state != LabDoorState.Locked)
            {
                return;
            }

            transitionElapsed = 0f;
            state = LabDoorState.Closed;
            ApplyStateImmediate();
        }

        private void BeginOpening()
        {
            transitionElapsed = 0f;
            state = LabDoorState.Opening;
            if (blockingCollider != null)
            {
                blockingCollider.enabled = true;
            }
        }

        private bool BeginClosing()
        {
            if (IsDoorwayObstructed())
            {
                return false;
            }

            transitionElapsed = 0f;
            state = LabDoorState.Closing;
            return true;
        }

        private bool IsDoorwayObstructed()
        {
            if (closingObstructionVolume == null)
            {
                return false;
            }

            Bounds bounds = closingObstructionVolume.bounds;
            int hitCount = Physics.OverlapBoxNonAlloc(
                bounds.center,
                bounds.extents,
                obstructionBuffer,
                closingObstructionVolume.transform.rotation,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hitCount; index++)
            {
                Collider candidate = obstructionBuffer[index];
                if (candidate == null || candidate.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (candidate.GetComponentInParent<CharacterController>() != null
                    || candidate.GetComponentInParent<PrototypeInteractable>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyStateImmediate()
        {
            bool isOpen = state == LabDoorState.Open;
            if (movingLeaf != null)
            {
                movingLeaf.localPosition = isOpen ? openLocalPosition : closedLocalPosition;
            }

            if (blockingCollider != null)
            {
                blockingCollider.enabled = !isOpen;
            }
        }
    }
}
