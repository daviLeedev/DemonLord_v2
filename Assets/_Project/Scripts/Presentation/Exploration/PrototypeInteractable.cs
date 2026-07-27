using System;
using UnityEngine;
using UnityEngine.Events;

namespace DemonLord.Presentation.Exploration
{
    public sealed class PrototypeInteractable : MonoBehaviour, IExplorationInteractable
    {
        [SerializeField] private string stableId = "interactable";
        [SerializeField] private string displayName = "대상";
        [SerializeField] private string actionLabel = "조사";
        [SerializeField] private Transform focusPoint = null;
        [SerializeField] private GameObject selectionMarker = null;
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private DialogueFocusController dialogueController = null;
        [SerializeField] private PlayerFacing facing = null;
        [SerializeField] private Transform dialogueCameraAnchor = null;
        [SerializeField] private float dialogueOrthographicSize = 5.5f;
        [SerializeField] private DialogueSequence dialogueSequence = null;
        [SerializeField] private string[] dialogueLines = Array.Empty<string>();
        [SerializeField] private UnityEvent onInteracted = new UnityEvent();

        public event Action<PrototypeInteractable> InteractionCompleted;

        public string StableId => stableId == null ? string.Empty : stableId.Trim();

        public string DisplayName => displayName ?? string.Empty;

        public string ActionLabel => actionLabel ?? string.Empty;

        public Transform FocusPoint => focusPoint != null ? focusPoint : transform;

        public Transform RootTransform => transform;

        public bool CanInteract => interactionEnabled && isActiveAndEnabled && gameObject.activeInHierarchy;

        public PlayerFacing Facing => facing;

        public Transform DialogueCameraAnchor => dialogueCameraAnchor;

        public float DialogueOrthographicSize => Mathf.Max(0.01f, dialogueOrthographicSize);

        public DialogueSequence DialogueSequence => dialogueSequence;

        public string[] DialogueLines => dialogueLines ?? Array.Empty<string>();

        private void Awake()
        {
            SetSelected(false);
        }

        private void OnDisable()
        {
            SetSelected(false);
            if (dialogueController != null)
            {
                dialogueController.NotifyInteractableUnavailable(this);
            }
        }

        public void BindDialogueController(DialogueFocusController configuredDialogueController)
        {
            dialogueController = configuredDialogueController;
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

            if (dialogueController != null
                && ((dialogueSequence != null && dialogueSequence.IsValid()) || DialogueLines.Length > 0))
            {
                return dialogueController.TryBeginDialogue(this, sensor);
            }

            onInteracted?.Invoke();
            InteractionCompleted?.Invoke(this);
            return true;
        }
    }
}
