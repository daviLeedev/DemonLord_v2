using System;
using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation.Exploration
{
    public sealed class DialogueFocusController : MonoBehaviour
    {
        [SerializeField] private ExplorationInputReader inputReader = null;
        [SerializeField] private PlayerFacing playerFacing = null;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private DialogueView dialogueView = null;
        [SerializeField] private CanvasGroup dialogueCanvasGroup = null;
        [SerializeField] private Text speakerLabel = null;
        [SerializeField] private Text lineLabel = null;

        private PrototypeInteractable activeInteractable;
        private DialogueSequence activeSequence;
        private InteractionSensor sourceSensor;
        private IDisposable gateToken;
        private PlayerFacingState playerFacingState;
        private PlayerFacingState npcFacingState;
        private bool hasPlayerFacingState;
        private bool hasNpcFacingState;
        private bool sessionActive;
        private int lineIndex;
        private int startedFrame = -1;
        private bool ending;
        private GameObject explorationHudRoot;
        private GameObject mapHudRoot;
        private GameObject interactionPromptRoot;
        private bool hudVisibilityCaptured;
        private bool explorationHudWasActive;
        private bool mapHudWasActive;
        private bool interactionPromptWasActive;

        public event Action<PrototypeInteractable> DialogueCompleted;

        public bool IsDialogueActive => sessionActive && !ending;

        public int CurrentLineIndex => IsDialogueActive ? lineIndex : -1;

        private void Awake()
        {
            if (playerRoot == null && playerFacing != null)
            {
                playerRoot = playerFacing.transform;
            }

            SetDialogueVisible(false);
        }

        private void OnEnable()
        {
            if (dialogueView != null)
            {
                dialogueView.PresentationDisabled += OnDialoguePresentationDisabled;
                dialogueView.AutoAdvanceRequested += OnAutoAdvanceRequested;
            }
        }

        private void Update()
        {
            if (!IsDialogueActive)
            {
                return;
            }

            if (activeInteractable == null || !activeInteractable.gameObject.activeInHierarchy)
            {
                EndDialogue();
                return;
            }

            if (dialogueView != null && !dialogueView.IsPresentationAvailable)
            {
                EndDialogue();
                return;
            }

            if (inputReader == null || Time.frameCount <= startedFrame)
            {
                return;
            }

            if (inputReader.ConsumeConfirmPressed())
            {
                AdvanceDialogue();
            }
        }

        private void OnDisable()
        {
            if (dialogueView != null)
            {
                dialogueView.PresentationDisabled -= OnDialoguePresentationDisabled;
                dialogueView.AutoAdvanceRequested -= OnAutoAdvanceRequested;
            }

            EndDialogue(false);
        }

        private void OnDestroy()
        {
            EndDialogue(false);
        }

        public bool TryBeginDialogue(PrototypeInteractable interactable, InteractionSensor sensor)
        {
            if (interactable == null || !interactable.CanInteract || IsDialogueActive || ending)
            {
                return false;
            }

            DialogueSequence sequence = interactable.DialogueSequence;
            string[] lines = interactable.DialogueLines;
            bool hasSequence = sequence != null && sequence.IsValid();
            if ((!hasSequence && (lines == null || lines.Length == 0))
                || inputReader == null
                || playerFacing == null)
            {
                return false;
            }

            Transform focus = interactable.FocusPoint;
            if (focus == null)
            {
                return false;
            }

            activeInteractable = interactable;
            activeSequence = hasSequence ? sequence : null;
            sourceSensor = sensor;
            sessionActive = true;
            lineIndex = 0;
            startedFrame = Time.frameCount;

            try
            {
                playerFacingState = playerFacing.CaptureState();
                hasPlayerFacingState = true;
                PlayerFacing npcFacing = interactable.Facing;
                if (npcFacing != null)
                {
                    npcFacingState = npcFacing.CaptureState();
                    hasNpcFacingState = true;
                }

                gateToken = inputReader.Gate.AcquireLock(
                    ExplorationInputChannel.Movement |
                    ExplorationInputChannel.Dash |
                    ExplorationInputChannel.Interaction |
                    ExplorationInputChannel.Camera);

                // F is both Interact and dialogue Confirm. Clear it so the interaction that
                // opened this conversation cannot advance its first visible frame. Escape is
                // deliberately owned by InGameUiCoordinator, not this controller.
                inputReader.ClearPendingDialogueInput();

                playerFacing.FaceTargetExact(focus.position);
                if (npcFacing != null)
                {
                    Transform player = playerRoot != null ? playerRoot : playerFacing.transform;
                    npcFacing.FaceTargetExact(player.position);
                }

                sourceSensor?.ClearSelection();
                RenderLine();
                SetDialogueVisible(true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                EndDialogue();
                return false;
            }
        }

        public void NotifyInteractableUnavailable(PrototypeInteractable interactable)
        {
            if (activeInteractable == interactable)
            {
                EndDialogue();
            }
        }

        public void AdvanceDialogue()
        {
            if (!IsDialogueActive)
            {
                return;
            }

            if (dialogueView != null && dialogueView.TryCloseHistory())
            {
                return;
            }

            if (dialogueView != null && dialogueView.TryCompleteReveal())
            {
                return;
            }

            lineIndex++;
            int lineCount = activeSequence != null ? activeSequence.LineCount : activeInteractable.DialogueLines.Length;
            if (lineIndex >= lineCount)
            {
                EndDialogue(true);
                return;
            }

            RenderLine();
        }

        public void EndDialogue()
        {
            if (dialogueView != null && dialogueView.TryCloseHistory())
            {
                return;
            }

            EndDialogue(false);
        }

        public void SkipDialogue()
        {
            if (!IsDialogueActive)
            {
                return;
            }

            EndDialogue(true);
        }

        private void EndDialogue(bool completed)
        {
            if (ending)
            {
                return;
            }

            if (!sessionActive && gateToken == null)
            {
                SetDialogueVisible(false);
                return;
            }

            ending = true;
            InteractionSensor sensorToRefresh = sourceSensor;
            PrototypeInteractable interactableToRestore = activeInteractable;
            try
            {
                SetDialogueVisible(false);
                if (hasPlayerFacingState && playerFacing != null)
                {
                    playerFacing.RestoreState(playerFacingState);
                }

                PlayerFacing npcFacing = interactableToRestore != null ? interactableToRestore.Facing : null;
                if (hasNpcFacingState && npcFacing != null)
                {
                    npcFacing.RestoreState(npcFacingState);
                }

                SafeDispose(ref gateToken);
            }
            finally
            {
                activeInteractable = null;
                activeSequence = null;
                sourceSensor = null;
                sessionActive = false;
                lineIndex = 0;
                startedFrame = -1;
                hasPlayerFacingState = false;
                hasNpcFacingState = false;
                ending = false;

                if (sensorToRefresh != null && sensorToRefresh.isActiveAndEnabled)
                {
                    sensorToRefresh.RefreshSelection();
                }

                if (completed && interactableToRestore != null)
                {
                    DialogueCompleted?.Invoke(interactableToRestore);
                }
            }
        }

        private void RenderLine()
        {
            if (activeInteractable == null)
            {
                return;
            }

            if (activeSequence != null)
            {
                dialogueView?.Show(activeSequence, lineIndex);
                DialogueLine dialogueLine = activeSequence.GetLine(lineIndex);
                DialogueParticipant participant = dialogueLine == null
                    ? null
                    : activeSequence.GetParticipant(dialogueLine.SpeakerSide);
                if (speakerLabel != null)
                {
                    speakerLabel.text = participant == null ? string.Empty : participant.DisplayName;
                }

                if (lineLabel != null)
                {
                    lineLabel.text = dialogueLine == null ? string.Empty : dialogueLine.Text;
                }

                return;
            }

            if (speakerLabel != null)
            {
                speakerLabel.text = activeInteractable.DisplayName;
            }

            string[] lines = activeInteractable.DialogueLines;
            if (lineLabel != null && lineIndex >= 0 && lineIndex < lines.Length)
            {
                lineLabel.text = lines[lineIndex] ?? string.Empty;
            }
        }

        private void SetDialogueVisible(bool visible)
        {
            SetExplorationHudVisible(!visible);
            if (dialogueView != null)
            {
                if (!visible)
                {
                    dialogueView.Hide();
                }
                else if (activeSequence != null)
                {
                    dialogueView.Show(activeSequence, lineIndex);
                }
            }

            // DialogueView owns the canvas state when it exists, including mouse
            // raycasts for its next/close buttons. The fallback remains input-only.
            if (dialogueCanvasGroup == null || dialogueView != null)
            {
                return;
            }

            dialogueCanvasGroup.alpha = visible ? 1f : 0f;
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }

        private void SetExplorationHudVisible(bool visible)
        {
            ResolveHudRoots();
            if (!visible)
            {
                if (!hudVisibilityCaptured)
                {
                    explorationHudWasActive = explorationHudRoot != null && explorationHudRoot.activeSelf;
                    mapHudWasActive = mapHudRoot != null && mapHudRoot.activeSelf;
                    interactionPromptWasActive = interactionPromptRoot != null && interactionPromptRoot.activeSelf;
                    hudVisibilityCaptured = true;
                }

                if (explorationHudRoot != null) explorationHudRoot.SetActive(false);
                if (mapHudRoot != null) mapHudRoot.SetActive(false);
                if (interactionPromptRoot != null) interactionPromptRoot.SetActive(false);
                return;
            }

            if (!hudVisibilityCaptured) return;
            if (explorationHudRoot != null) explorationHudRoot.SetActive(explorationHudWasActive);
            if (mapHudRoot != null) mapHudRoot.SetActive(mapHudWasActive);
            if (interactionPromptRoot != null) interactionPromptRoot.SetActive(interactionPromptWasActive);
            hudVisibilityCaptured = false;
        }

        private void ResolveHudRoots()
        {
            Transform canvasRoot = transform;
            explorationHudRoot ??= canvasRoot.Find("SafeAreaRoot")?.gameObject;
            mapHudRoot ??= canvasRoot.Find("__AreaSystemMapUi")?.gameObject;
            interactionPromptRoot ??= canvasRoot.Find("InteractionPrompt")?.gameObject;
        }

        private void OnDialoguePresentationDisabled()
        {
            if (IsDialogueActive)
            {
                EndDialogue();
            }
        }

        private void OnAutoAdvanceRequested()
        {
            if (IsDialogueActive) AdvanceDialogue();
        }

        private static void SafeDispose(ref IDisposable handle)
        {
            IDisposable disposable = handle;
            handle = null;
            if (disposable == null)
            {
                return;
            }

            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
