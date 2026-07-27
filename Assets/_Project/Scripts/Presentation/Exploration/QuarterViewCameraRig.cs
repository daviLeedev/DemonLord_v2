using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class QuarterViewCameraRig : MonoBehaviour
    {
        private const float SnapEpsilon = 0.01f;

        [SerializeField] private QuarterViewCameraProfile defaultProfile = new QuarterViewCameraProfile();
        [SerializeField, Min(0.01f)] private float cameraDistance = 20f;
        [SerializeField, Min(0f)] private float followDamping = 0.12f;
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.03f;
        [SerializeField, Min(0f)] private float dialogueTransitionDuration = 0.25f;

        private readonly List<ActiveZone> activeZones = new List<ActiveZone>();
        private readonly List<DialogueOverrideContext> dialogueOverrides = new List<DialogueOverrideContext>();
        private Camera gameCamera;
        private Transform target;
        private ExplorationInputReader inputReader;
        private ExplorationInputGate inputGate;
        private CameraZone selectedZone;
        private bool configured;
        private int quarterIndex;
        private long zoneEntrySequence;
        private float zoomOffset;
        private float currentYaw;
        private float currentPitch;
        private float currentOrthographicSize;
        private float pitchVelocity;
        private float zoomVelocity;
        private Vector3 currentFollowOffset;
        private Vector3 followOffsetVelocity;
        private Vector3 currentFocusPosition;
        private Vector3 focusVelocity;
        private float yawTransitionStart;
        private float yawTransitionTarget;
        private float yawTransitionElapsed;
        private float yawTransitionDuration;
        private bool yawTransitionActive;

        public Transform MovementBasis => gameCamera == null ? null : gameCamera.transform;

        public Camera GameCamera => gameCamera;

        public float CurrentYawDegrees => currentYaw;

        public int QuarterIndex => quarterIndex;

        public void Configure(
            Camera configuredCamera,
            Transform configuredTarget,
            ExplorationInputReader configuredInputReader,
            ExplorationInputGate configuredInputGate)
        {
            if (configuredCamera == null)
            {
                throw new ArgumentNullException(nameof(configuredCamera));
            }

            if (configuredTarget == null)
            {
                throw new ArgumentNullException(nameof(configuredTarget));
            }

            if (configuredInputReader == null)
            {
                throw new ArgumentNullException(nameof(configuredInputReader));
            }

            if (configuredInputGate == null)
            {
                throw new ArgumentNullException(nameof(configuredInputGate));
            }

            if (!ReferenceEquals(configuredInputReader.Gate, configuredInputGate))
            {
                throw new ArgumentException("The camera and input reader must use the same input gate.", nameof(configuredInputGate));
            }

            gameCamera = configuredCamera;
            inputReader = configuredInputReader;
            inputGate = configuredInputGate;
            gameCamera.orthographic = true;
            quarterIndex = 0;
            zoomOffset = 0f;
            configured = true;
            SetTarget(configuredTarget);
            SnapImmediate();
        }

        public void SetTarget(Transform newTarget)
        {
            if (newTarget == null)
            {
                throw new ArgumentNullException(nameof(newTarget));
            }

            target = newTarget;
            focusVelocity = Vector3.zero;
        }

        public void SnapImmediate()
        {
            EnsureConfigured();

            if (dialogueOverrides.Count > 0)
            {
                DialogueOverrideContext dialogue = dialogueOverrides[dialogueOverrides.Count - 1];
                dialogue.ResolvePose(out Vector3 position, out Quaternion rotation);
                gameCamera.transform.SetPositionAndRotation(position, rotation);
                gameCamera.orthographicSize = dialogue.OrthographicSize;
                return;
            }

            QuarterViewCameraProfile profile = ActiveProfile;
            currentYaw = profile.ResolveYawDegrees(quarterIndex);
            currentPitch = profile.PitchDegrees;
            currentOrthographicSize = ResolveTargetOrthographicSize(profile);
            currentFollowOffset = profile.FollowOffset;
            currentFocusPosition = target.position + currentFollowOffset;
            yawTransitionStart = currentYaw;
            yawTransitionTarget = currentYaw;
            yawTransitionElapsed = 0f;
            yawTransitionDuration = 0f;
            yawTransitionActive = false;
            pitchVelocity = 0f;
            zoomVelocity = 0f;
            followOffsetVelocity = Vector3.zero;
            focusVelocity = Vector3.zero;
            ApplyExplorationTransform();
        }

        public IDisposable PushDialogueOverride(
            Transform player,
            Transform focus,
            Transform anchor,
            float orthographicSize)
        {
            EnsureConfigured();
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (focus == null)
            {
                throw new ArgumentNullException(nameof(focus));
            }

            IDisposable cameraLock = inputGate.AcquireLock(ExplorationInputChannel.Camera);
            DialogueOverrideContext context = new DialogueOverrideContext(
                anchor,
                ResolveDialogueFallbackPosition(player, focus),
                ResolveDialogueFallbackRotation(player, focus),
                Mathf.Clamp(
                    orthographicSize,
                    QuarterViewCameraProfile.MinimumOrthographicSize,
                    QuarterViewCameraProfile.MaximumOrthographicSize),
                cameraLock);
            dialogueOverrides.Add(context);
            return new DialogueOverrideHandle(this, context);
        }

        internal bool IsTargetCollider(Collider candidate)
        {
            if (!configured || candidate == null || target == null)
            {
                return false;
            }

            Transform candidateTransform = candidate.transform;
            return candidateTransform == target || candidateTransform.IsChildOf(target);
        }

        internal void NotifyZoneEntered(CameraZone zone)
        {
            if (zone == null)
            {
                return;
            }

            for (int index = activeZones.Count - 1; index >= 0; index--)
            {
                if (activeZones[index].Zone == zone)
                {
                    activeZones.RemoveAt(index);
                }
            }

            activeZones.Add(new ActiveZone(zone, ++zoneEntrySequence));
            RefreshSelectedZone();
        }

        internal void NotifyZoneExited(CameraZone zone)
        {
            if (zone == null)
            {
                return;
            }

            for (int index = activeZones.Count - 1; index >= 0; index--)
            {
                if (activeZones[index].Zone == zone)
                {
                    activeZones.RemoveAt(index);
                }
            }

            RefreshSelectedZone();
        }

        private QuarterViewCameraProfile ActiveProfile
        {
            get
            {
                QuarterViewCameraProfile profile = selectedZone == null ? null : selectedZone.Profile;
                return profile ?? defaultProfile ?? new QuarterViewCameraProfile();
            }
        }

        private void LateUpdate()
        {
            if (!configured || gameCamera == null || target == null)
            {
                return;
            }

            ConsumeCameraInput();
            float deltaTime = Time.unscaledDeltaTime;
            if (dialogueOverrides.Count > 0)
            {
                ApplyDialogueCamera(dialogueOverrides[dialogueOverrides.Count - 1], deltaTime);
                return;
            }

            ApplyExplorationCamera(deltaTime);
        }

        private void OnDisable()
        {
            ReleaseAllDialogueOverrides();
        }

        private void ConsumeCameraInput()
        {
            if (inputReader == null || inputGate == null)
            {
                return;
            }

            int rotationSteps = inputReader.ConsumeCameraRotationSteps();
            float zoomDelta = inputReader.ConsumeZoomDelta();
            if (inputGate.IsBlocked(ExplorationInputChannel.Camera) || dialogueOverrides.Count > 0)
            {
                return;
            }

            if (rotationSteps != 0)
            {
                quarterIndex = NormalizeQuarterIndex(quarterIndex + rotationSteps);
            }

            if (Mathf.Abs(zoomDelta) > Mathf.Epsilon)
            {
                QuarterViewCameraProfile profile = ActiveProfile;
                float requestedSize = Mathf.Clamp(
                    profile.OrthographicSize + zoomOffset - zoomDelta * zoomSensitivity,
                    QuarterViewCameraProfile.MinimumOrthographicSize,
                    QuarterViewCameraProfile.MaximumOrthographicSize);
                zoomOffset = requestedSize - profile.OrthographicSize;
            }
        }

        private void ApplyExplorationCamera(float deltaTime)
        {
            QuarterViewCameraProfile profile = ActiveProfile;
            float targetYaw = profile.ResolveYawDegrees(quarterIndex);
            float targetPitch = profile.PitchDegrees;
            float targetOrthographicSize = ResolveTargetOrthographicSize(profile);
            UpdateYaw(targetYaw, profile.TransitionDuration, deltaTime);

            float smoothTime = profile.TransitionDuration;
            if (smoothTime <= Mathf.Epsilon || deltaTime <= Mathf.Epsilon)
            {
                currentPitch = targetPitch;
                currentOrthographicSize = targetOrthographicSize;
                currentFollowOffset = profile.FollowOffset;
            }
            else
            {
                currentPitch = Mathf.SmoothDampAngle(
                    currentPitch,
                    targetPitch,
                    ref pitchVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    deltaTime);
                currentOrthographicSize = Mathf.SmoothDamp(
                    currentOrthographicSize,
                    targetOrthographicSize,
                    ref zoomVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    deltaTime);
                currentFollowOffset = Vector3.SmoothDamp(
                    currentFollowOffset,
                    profile.FollowOffset,
                    ref followOffsetVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    deltaTime);
            }

            Vector3 desiredFocus = target.position + currentFollowOffset;
            if (followDamping <= Mathf.Epsilon || deltaTime <= Mathf.Epsilon)
            {
                currentFocusPosition = desiredFocus;
            }
            else
            {
                currentFocusPosition = Vector3.SmoothDamp(
                    currentFocusPosition,
                    desiredFocus,
                    ref focusVelocity,
                    followDamping,
                    Mathf.Infinity,
                    deltaTime);
            }

            ApplyExplorationTransform();
        }

        private void ApplyExplorationTransform()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 cameraPosition = currentFocusPosition - rotation * Vector3.forward * Mathf.Max(0.01f, cameraDistance);
            gameCamera.transform.SetPositionAndRotation(cameraPosition, rotation);
            gameCamera.orthographicSize = Mathf.Clamp(
                currentOrthographicSize,
                QuarterViewCameraProfile.MinimumOrthographicSize,
                QuarterViewCameraProfile.MaximumOrthographicSize);
        }

        private void UpdateYaw(float targetYaw, float transitionDuration, float deltaTime)
        {
            bool targetChanged = Mathf.Abs(Mathf.DeltaAngle(yawTransitionTarget, targetYaw)) > SnapEpsilon;
            bool needsTransition = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw)) > SnapEpsilon;
            if (!yawTransitionActive && !needsTransition)
            {
                currentYaw = targetYaw;
                return;
            }

            if (!yawTransitionActive || targetChanged)
            {
                yawTransitionStart = currentYaw;
                yawTransitionTarget = targetYaw;
                yawTransitionElapsed = 0f;
                yawTransitionDuration = Mathf.Max(0f, transitionDuration);
                yawTransitionActive = yawTransitionDuration > Mathf.Epsilon;
            }

            if (!yawTransitionActive || deltaTime <= Mathf.Epsilon)
            {
                currentYaw = targetYaw;
                yawTransitionActive = false;
                return;
            }

            yawTransitionElapsed = Mathf.Min(yawTransitionDuration, yawTransitionElapsed + deltaTime);
            float normalizedTime = Mathf.Clamp01(yawTransitionElapsed / yawTransitionDuration);
            float easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
            currentYaw = Mathf.LerpAngle(yawTransitionStart, yawTransitionTarget, easedTime);
            if (normalizedTime >= 1f)
            {
                currentYaw = yawTransitionTarget;
                yawTransitionActive = false;
            }
        }

        private void ApplyDialogueCamera(DialogueOverrideContext dialogue, float deltaTime)
        {
            float blend = dialogueTransitionDuration <= Mathf.Epsilon || deltaTime <= Mathf.Epsilon
                ? 1f
                : 1f - Mathf.Exp(-deltaTime / dialogueTransitionDuration);
            Transform cameraTransform = gameCamera.transform;
            dialogue.ResolvePose(out Vector3 targetPosition, out Quaternion targetRotation);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPosition, blend);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, blend);
            gameCamera.orthographicSize = Mathf.Lerp(gameCamera.orthographicSize, dialogue.OrthographicSize, blend);
        }

        private float ResolveTargetOrthographicSize(QuarterViewCameraProfile profile)
        {
            return Mathf.Clamp(
                profile.OrthographicSize + zoomOffset,
                QuarterViewCameraProfile.MinimumOrthographicSize,
                QuarterViewCameraProfile.MaximumOrthographicSize);
        }

        private void RefreshSelectedZone()
        {
            ActiveZone best = null;
            for (int index = activeZones.Count - 1; index >= 0; index--)
            {
                ActiveZone candidate = activeZones[index];
                if (candidate.Zone == null || !candidate.Zone.isActiveAndEnabled)
                {
                    activeZones.RemoveAt(index);
                    continue;
                }

                if (best == null || IsHigherPriority(candidate, best))
                {
                    best = candidate;
                }
            }

            selectedZone = best == null ? null : best.Zone;
        }

        private static bool IsHigherPriority(ActiveZone candidate, ActiveZone current)
        {
            if (candidate.Zone.Priority != current.Zone.Priority)
            {
                return candidate.Zone.Priority > current.Zone.Priority;
            }

            if (candidate.EntrySequence != current.EntrySequence)
            {
                return candidate.EntrySequence > current.EntrySequence;
            }

            return string.CompareOrdinal(candidate.Zone.StableId, current.Zone.StableId) < 0;
        }

        private void ReleaseDialogueOverride(DialogueOverrideContext context)
        {
            int index = dialogueOverrides.IndexOf(context);
            if (index < 0)
            {
                return;
            }

            dialogueOverrides.RemoveAt(index);
            context.ReleaseLock();
        }

        private void ReleaseAllDialogueOverrides()
        {
            for (int index = dialogueOverrides.Count - 1; index >= 0; index--)
            {
                dialogueOverrides[index].ReleaseLock();
            }

            dialogueOverrides.Clear();
        }

        private void EnsureConfigured()
        {
            if (!configured || gameCamera == null || target == null || inputReader == null || inputGate == null)
            {
                throw new InvalidOperationException("QuarterViewCameraRig has not been configured.");
            }
        }

        private static int NormalizeQuarterIndex(int value)
        {
            int normalized = value % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }

        private Vector3 ResolveDialogueFallbackPosition(Transform player, Transform focus)
        {
            Vector3 midpoint = Vector3.Lerp(player.position, focus.position, 0.5f);
            midpoint.y = Mathf.Max(player.position.y, focus.position.y) + 1.2f;
            Quaternion rotation = ResolveDialogueFallbackRotation(player, focus);
            return midpoint - rotation * Vector3.forward * Mathf.Max(0.01f, cameraDistance * 0.65f);
        }

        private Quaternion ResolveDialogueFallbackRotation(Transform player, Transform focus)
        {
            if (gameCamera != null)
            {
                return gameCamera.transform.rotation;
            }

            Vector3 horizontalDirection = focus.position - player.position;
            horizontalDirection.y = 0f;
            float yaw = horizontalDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up).eulerAngles.y - 90f
                : ActiveProfile.ResolveYawDegrees(quarterIndex);
            return Quaternion.Euler(ActiveProfile.PitchDegrees, yaw, 0f);
        }

        private sealed class ActiveZone
        {
            public ActiveZone(CameraZone zone, long entrySequence)
            {
                Zone = zone;
                EntrySequence = entrySequence;
            }

            public CameraZone Zone { get; }

            public long EntrySequence { get; }
        }

        private sealed class DialogueOverrideContext
        {
            private IDisposable cameraLock;
            private bool released;

            public DialogueOverrideContext(
                Transform anchor,
                Vector3 fallbackPosition,
                Quaternion fallbackRotation,
                float orthographicSize,
                IDisposable cameraLock)
            {
                Anchor = anchor;
                FallbackPosition = fallbackPosition;
                FallbackRotation = fallbackRotation;
                OrthographicSize = orthographicSize;
                this.cameraLock = cameraLock;
            }

            public Transform Anchor { get; }

            private Vector3 FallbackPosition { get; }

            private Quaternion FallbackRotation { get; }

            public float OrthographicSize { get; }

            public void ResolvePose(out Vector3 position, out Quaternion rotation)
            {
                if (Anchor != null)
                {
                    position = Anchor.position;
                    rotation = Anchor.rotation;
                    return;
                }

                position = FallbackPosition;
                rotation = FallbackRotation;
            }

            public void ReleaseLock()
            {
                if (released)
                {
                    return;
                }

                released = true;
                cameraLock?.Dispose();
                cameraLock = null;
            }
        }

        private sealed class DialogueOverrideHandle : IDisposable
        {
            private QuarterViewCameraRig owner;
            private DialogueOverrideContext context;

            public DialogueOverrideHandle(QuarterViewCameraRig owner, DialogueOverrideContext context)
            {
                this.owner = owner;
                this.context = context;
            }

            public void Dispose()
            {
                if (owner != null && context != null)
                {
                    owner.ReleaseDialogueOverride(context);
                }

                owner = null;
                context = null;
            }
        }
    }
}
