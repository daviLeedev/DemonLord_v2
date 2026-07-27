using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public sealed class InteractionSensor : MonoBehaviour
    {
        private const int CandidateBufferSize = 32;
        private const int LineOfSightBufferSize = 16;
        private const float LineOfSightEndTolerance = 0.02f;

        [SerializeField] private ExplorationInputReader inputReader = null;
        [SerializeField] private PlayerFacing playerFacing = null;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform sensorOrigin;
        [SerializeField] private InteractionPromptView promptView = null;
        [SerializeField] private float interactionRadius = 2.2f;
        [SerializeField] private float forwardConeAngle = 100f;
        [SerializeField] private LayerMask candidateMask = 1 << Physics.IgnoreRaycastLayer;
        [SerializeField] private LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;

        private readonly Collider[] candidateBuffer = new Collider[CandidateBufferSize];
        private readonly RaycastHit[] lineOfSightBuffer = new RaycastHit[LineOfSightBufferSize];
        private readonly HashSet<EntityId> seenComponents = new HashSet<EntityId>();
        private IExplorationInteractable current;
        private Component currentComponent;

        public event Action<IExplorationInteractable> SelectionChanged;

        public IExplorationInteractable Current => IsAlive(current, currentComponent) ? current : null;

        private void Awake()
        {
            if (sensorOrigin == null)
            {
                sensorOrigin = transform;
            }

            if (playerRoot == null && playerFacing != null)
            {
                playerRoot = playerFacing.transform;
            }
        }

        private void Update()
        {
            if (IsInteractionBlocked())
            {
                ClearSelection();
                return;
            }

            RefreshSelection();
            if (inputReader != null && inputReader.ConsumeInteractPressed())
            {
                TryInteractCurrent();
            }
        }

        private void OnDisable()
        {
            ClearSelection();
        }

        public void RefreshSelection()
        {
            if (!isActiveAndEnabled || IsInteractionBlocked())
            {
                ClearSelection();
                return;
            }

            Vector3 origin = sensorOrigin != null ? sensorOrigin.position : transform.position;
            Vector3 facing = playerFacing != null ? playerFacing.CurrentWorldDirection : transform.forward;
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                Mathf.Max(0.01f, interactionRadius),
                candidateBuffer,
                candidateMask,
                QueryTriggerInteraction.Collide);

            seenComponents.Clear();
            IExplorationInteractable best = null;
            Component bestComponent = null;
            InteractionCandidateScore bestScore = default;
            bool hasBest = false;

            for (int index = 0; index < hitCount; index++)
            {
                Collider candidateCollider = candidateBuffer[index];
                if (candidateCollider == null)
                {
                    continue;
                }

                Component component = candidateCollider.GetComponentInParent(typeof(IExplorationInteractable));
                if (component == null || !seenComponents.Add(component.GetEntityId()))
                {
                    continue;
                }

                IExplorationInteractable candidate = component as IExplorationInteractable;
                if (!IsAlive(candidate, component) || !candidate.CanInteract)
                {
                    continue;
                }

                Transform focusPoint = candidate.FocusPoint;
                if (focusPoint == null || !InteractionSelection.TryCreateScore(
                        origin,
                        facing,
                        focusPoint.position,
                        interactionRadius,
                        forwardConeAngle,
                        candidate.StableId,
                        out InteractionCandidateScore score))
                {
                    continue;
                }

                if (!HasLineOfSight(origin, focusPoint.position, component.transform))
                {
                    continue;
                }

                if (!hasBest || InteractionSelection.IsBetter(score, bestScore))
                {
                    best = candidate;
                    bestComponent = component;
                    bestScore = score;
                    hasBest = true;
                }
            }

            SetSelection(best, bestComponent);
        }

        public bool TryInteractCurrent()
        {
            IExplorationInteractable selected = Current;
            if (selected == null || IsInteractionBlocked() || !selected.CanInteract)
            {
                return false;
            }

            return selected.TryInteract(this);
        }

        public void ClearSelection()
        {
            SetSelection(null, null);
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 target, Transform targetRoot)
        {
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= LineOfSightEndTolerance)
            {
                return true;
            }

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction / distance,
                lineOfSightBuffer,
                distance,
                lineOfSightMask,
                QueryTriggerInteraction.Ignore);
            float closestBlockingDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                Collider hitCollider = lineOfSightBuffer[index].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                Transform hitTransform = hitCollider.transform;
                Transform selfRoot = playerRoot != null ? playerRoot : transform;
                if (hitTransform.IsChildOf(selfRoot) || selfRoot.IsChildOf(hitTransform))
                {
                    continue;
                }

                if (targetRoot != null &&
                    (hitTransform.IsChildOf(targetRoot) || targetRoot.IsChildOf(hitTransform)))
                {
                    continue;
                }

                closestBlockingDistance = Mathf.Min(closestBlockingDistance, lineOfSightBuffer[index].distance);
            }

            return closestBlockingDistance >= distance - LineOfSightEndTolerance;
        }

        private bool IsInteractionBlocked()
        {
            return inputReader != null &&
                   inputReader.Gate.IsBlocked(ExplorationInputChannel.Interaction);
        }

        private void SetSelection(IExplorationInteractable next, Component nextComponent)
        {
            if (ReferenceEquals(current, next) && currentComponent == nextComponent)
            {
                if (Current == null && current != null)
                {
                    current = null;
                    currentComponent = null;
                    promptView?.Hide();
                    SelectionChanged?.Invoke(null);
                }

                return;
            }

            if (IsAlive(current, currentComponent))
            {
                current.SetSelected(false);
            }

            current = next;
            currentComponent = nextComponent;
            if (IsAlive(current, currentComponent))
            {
                current.SetSelected(true);
                promptView?.Show(current);
            }
            else
            {
                current = null;
                currentComponent = null;
                promptView?.Hide();
            }

            SelectionChanged?.Invoke(current);
        }

        private static bool IsAlive(IExplorationInteractable interactable, Component component)
        {
            return interactable != null && component != null && component.gameObject.activeInHierarchy;
        }
    }
}
