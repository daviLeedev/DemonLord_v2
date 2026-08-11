using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649 // Serialized Unity references are assigned by the scene builder/inspector.

namespace DemonLord.Presentation.Exploration
{
    [DisallowMultipleComponent]
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image playerPortrait;
        [SerializeField] private Image partnerPortrait;
        [SerializeField] private Image playerNameplate;
        [SerializeField] private Image partnerNameplate;
        [SerializeField] private Text playerNameLabel;
        [SerializeField] private Text partnerNameLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private CanvasGroup historyGroup;
        [SerializeField] private Text historyLabel;
        [SerializeField] private Text autoModeLabel;
        [SerializeField] private DialogueTheme theme;
        [SerializeField, Min(1f)] private float charactersPerSecond = 48f;
        [SerializeField, Min(0.1f)] private float autoAdvanceDelay = 1.25f;
        [SerializeField, Min(0.01f)] private float portraitTransitionSeconds = 0.18f;
        [SerializeField, Min(0f)] private float portraitSlideDistance = 28f;

        private Coroutine revealRoutine;
        private Coroutine playerPortraitRoutine;
        private Coroutine partnerPortraitRoutine;
        private string fullBodyText = string.Empty;
        private bool isRevealing;
        private readonly List<string> history = new List<string>();
        private DialogueSequence lastHistorySequence;
        private int lastHistoryLineIndex = -1;
        private bool historyVisible;
        private bool autoMode;
        private float autoCountdown = -1f;
        private bool portraitPositionsCaptured;
        private Vector2 playerPortraitPosition;
        private Vector2 partnerPortraitPosition;

        public event Action PresentationDisabled;
        public event Action AutoAdvanceRequested;

        public bool IsPresentationAvailable => panelRoot != null
            && panelRoot.activeInHierarchy
            && canvasGroup != null
            && isActiveAndEnabled;

        private void Awake()
        {
            ApplyTheme();
            NormalizeToolbarLayout();
            CapturePortraitPositions();
            SetHistoryVisible(false);
            Hide();
        }

        private void Update()
        {
            if (!autoMode || historyVisible || isRevealing || autoCountdown < 0f
                || !IsPresentationAvailable)
            {
                return;
            }

            autoCountdown -= Time.unscaledDeltaTime;
            if (autoCountdown > 0f) return;
            autoCountdown = -1f;
            AutoAdvanceRequested?.Invoke();
        }

        private void OnDisable()
        {
            StopReveal();
            StopPortraitTransitions();
            PresentationDisabled?.Invoke();
        }

        public void Show(DialogueSequence sequence, int lineIndex)
        {
            if (sequence == null || !sequence.IsValid() || !IsPresentationAvailable)
            {
                return;
            }

            DialogueLine line = sequence.GetLine(lineIndex);
            if (line == null)
            {
                return;
            }

            ApplyTheme();
            ApplyParticipant(playerPortrait, playerNameplate, playerNameLabel, sequence.Player, true, line.SpeakerSide == DialogueSpeakerSide.Player);
            ApplyParticipant(partnerPortrait, partnerNameplate, partnerNameLabel, sequence.Partner, false, line.SpeakerSide == DialogueSpeakerSide.Partner);
            AnimateSpeakingPortrait(line.SpeakerSide);
            AppendHistory(sequence, lineIndex, line);

            if (bodyLabel != null)
            {
                StartReveal(line.Text);
            }

            if (hintLabel != null)
            {
                hintLabel.text = "F / Enter 계속   첫 입력 즉시 완성   Esc 닫기";
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            StopReveal();
            StopPortraitTransitions();
            autoCountdown = -1f;
            SetHistoryVisible(false);
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public bool TryCompleteReveal()
        {
            if (!isRevealing || bodyLabel == null)
            {
                return false;
            }

            StopReveal();
            bodyLabel.text = fullBodyText;
            ScheduleAutoAdvance();
            return true;
        }

        public void ToggleAuto()
        {
            autoMode = !autoMode;
            if (autoModeLabel != null) autoModeLabel.text = autoMode ? "자동 ON" : "자동 OFF";
            if (autoMode && !isRevealing) ScheduleAutoAdvance();
            else if (!autoMode) autoCountdown = -1f;
        }

        public void ToggleHistory()
        {
            SetHistoryVisible(!historyVisible);
        }

        public bool TryCloseHistory()
        {
            if (!historyVisible) return false;
            SetHistoryVisible(false);
            return true;
        }

        private void StartReveal(string text)
        {
            StopReveal();
            autoCountdown = -1f;
            fullBodyText = text ?? string.Empty;
            if (bodyLabel == null)
            {
                return;
            }

            bodyLabel.text = string.Empty;
            if (fullBodyText.Length == 0 || charactersPerSecond <= 0f || !isActiveAndEnabled)
            {
                bodyLabel.text = fullBodyText;
                return;
            }

            isRevealing = true;
            revealRoutine = StartCoroutine(RevealBody());
        }

        private IEnumerator RevealBody()
        {
            float visibleCharacters = 0f;
            while (bodyLabel != null && visibleCharacters < fullBodyText.Length)
            {
                visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                int count = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, fullBodyText.Length);
                bodyLabel.text = fullBodyText.Substring(0, count);
                yield return null;
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = fullBodyText;
            }

            revealRoutine = null;
            isRevealing = false;
            ScheduleAutoAdvance();
        }

        private void StopReveal()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }

            isRevealing = false;
        }

        private void ScheduleAutoAdvance()
        {
            autoCountdown = autoMode ? autoAdvanceDelay : -1f;
        }

        private void AppendHistory(DialogueSequence sequence, int lineIndex, DialogueLine line)
        {
            if (sequence == lastHistorySequence && lineIndex == lastHistoryLineIndex) return;
            lastHistorySequence = sequence;
            lastHistoryLineIndex = lineIndex;
            DialogueParticipant speaker = sequence.GetParticipant(line.SpeakerSide);
            string speakerName = speaker == null ? string.Empty : speaker.DisplayName;
            history.Add(string.IsNullOrWhiteSpace(speakerName)
                ? line.Text
                : speakerName + "\n" + line.Text);
            while (history.Count > 20) history.RemoveAt(0);
            if (historyLabel != null) historyLabel.text = string.Join("\n\n", history);
        }

        private void SetHistoryVisible(bool visible)
        {
            historyVisible = visible;
            if (historyGroup == null) return;
            historyGroup.alpha = visible ? 1f : 0f;
            historyGroup.interactable = visible;
            historyGroup.blocksRaycasts = visible;
        }

        private void CapturePortraitPositions()
        {
            if (portraitPositionsCaptured) return;
            if (playerPortrait != null) playerPortraitPosition = playerPortrait.rectTransform.anchoredPosition;
            if (partnerPortrait != null) partnerPortraitPosition = partnerPortrait.rectTransform.anchoredPosition;
            portraitPositionsCaptured = true;
        }

        private void AnimateSpeakingPortrait(DialogueSpeakerSide side)
        {
            CapturePortraitPositions();
            if (side == DialogueSpeakerSide.Player && playerPortrait != null && playerPortrait.enabled)
            {
                if (playerPortraitRoutine != null) StopCoroutine(playerPortraitRoutine);
                playerPortraitRoutine = StartCoroutine(AnimatePortrait(playerPortrait, playerPortraitPosition, -portraitSlideDistance, true));
            }
            else if (side == DialogueSpeakerSide.Partner && partnerPortrait != null && partnerPortrait.enabled)
            {
                if (partnerPortraitRoutine != null) StopCoroutine(partnerPortraitRoutine);
                partnerPortraitRoutine = StartCoroutine(AnimatePortrait(partnerPortrait, partnerPortraitPosition, portraitSlideDistance, false));
            }
        }

        private IEnumerator AnimatePortrait(Image portrait, Vector2 targetPosition, float offset, bool player)
        {
            RectTransform rect = portrait.rectTransform;
            Color targetColor = portrait.color;
            Color startColor = targetColor;
            startColor.a *= 0.35f;
            rect.anchoredPosition = targetPosition + Vector2.right * offset;
            portrait.color = startColor;
            float elapsed = 0f;
            while (portrait != null && elapsed < portraitTransitionSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / portraitTransitionSeconds);
                float eased = progress * progress * (3f - 2f * progress);
                rect.anchoredPosition = Vector2.Lerp(targetPosition + Vector2.right * offset, targetPosition, eased);
                portrait.color = Color.Lerp(startColor, targetColor, eased);
                yield return null;
            }

            if (portrait != null)
            {
                rect.anchoredPosition = targetPosition;
                portrait.color = targetColor;
            }

            if (player) playerPortraitRoutine = null;
            else partnerPortraitRoutine = null;
        }

        private void StopPortraitTransitions()
        {
            if (playerPortraitRoutine != null) StopCoroutine(playerPortraitRoutine);
            if (partnerPortraitRoutine != null) StopCoroutine(partnerPortraitRoutine);
            playerPortraitRoutine = null;
            partnerPortraitRoutine = null;
            if (playerPortrait != null) playerPortrait.rectTransform.anchoredPosition = playerPortraitPosition;
            if (partnerPortrait != null) partnerPortrait.rectTransform.anchoredPosition = partnerPortraitPosition;
        }

        private void ApplyTheme()
        {
            if (theme == null)
            {
                return;
            }

            ApplyTextTheme(bodyLabel, theme.BodyFontSize, theme.BodyColor);
            ApplyTextTheme(playerNameLabel, theme.NameFontSize, theme.PlayerAccent);
            ApplyTextTheme(partnerNameLabel, theme.NameFontSize, theme.PartnerAccent);
            ApplyTextTheme(hintLabel, Mathf.Max(18, theme.NameFontSize - 7), theme.BodyColor);
        }

        private void NormalizeToolbarLayout()
        {
            Transform dialoguePanel = bodyLabel != null ? bodyLabel.transform.parent : null;
            if (dialoguePanel == null) return;

            if (bodyLabel != null)
            {
                RectTransform bodyRect = bodyLabel.rectTransform;
                bodyRect.anchoredPosition = new Vector2(270f, 235f);
                bodyRect.sizeDelta = new Vector2(1120f, 100f);
            }

            const float toolbarY = 155f;
            SetToolbarRect(dialoguePanel, "ContinuePromptDecoration", 170f, 360f, toolbarY, null, true);
            SetToolbarRect(dialoguePanel, "HistoryButton", 555f, 180f, toolbarY, "대화 기록");
            SetToolbarRect(dialoguePanel, "AutoButton", 750f, 180f, toolbarY, "자동 OFF");
            SetToolbarRect(dialoguePanel, "SkipButton", 945f, 180f, toolbarY, "전체 스킵");
            SetToolbarRect(dialoguePanel, "CloseButton", 1140f, 190f, toolbarY, "닫기");
            SetToolbarRect(dialoguePanel, "AdvanceButton", 1345f, 190f, toolbarY, "다음");
        }

        private static void SetToolbarRect(
            Transform panel,
            string childName,
            float x,
            float width,
            float y,
            string labelText,
            bool isHint = false)
        {
            RectTransform rect = panel.Find(childName) as RectTransform;
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, 54f);

            Text label = rect.GetComponentInChildren<Text>(true);
            if (label == null) return;

            label.gameObject.SetActive(true);
            label.enabled = true;
            if (!string.IsNullOrEmpty(labelText))
            {
                label.text = labelText;
            }

            label.color = new Color(0.97f, 0.92f, 0.78f, 1f);
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = isHint ? 13 : 15;
            label.resizeTextMaxSize = isHint ? 18 : 20;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(-24f, -8f);
            labelRect.SetAsLastSibling();
        }

        private void ApplyParticipant(
            Image portraitImage,
            Image nameplateImage,
            Text nameLabel,
            DialogueParticipant participant,
            bool isPlayer,
            bool isSpeaking)
        {
            if (participant == null)
            {
                return;
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = participant.Portrait;
                portraitImage.enabled = participant.Portrait != null;
                portraitImage.preserveAspect = true;
                Color color = Color.white;
                color.a = isSpeaking ? 1f : theme != null ? theme.InactivePortraitAlpha : 0.45f;
                portraitImage.color = color;
            }

            if (nameplateImage != null)
            {
                Color color = Color.white;
                color.a = isSpeaking ? 1f : 0.45f;
                nameplateImage.color = color;
            }

            if (nameLabel != null)
            {
                nameLabel.text = participant.DisplayName;
                Color color = isPlayer
                    ? theme != null ? theme.PlayerAccent : Color.cyan
                    : theme != null ? theme.PartnerAccent : Color.magenta;
                color.a = isSpeaking ? 1f : 0.62f;
                nameLabel.color = color;
            }
        }

        private static void ApplyTextTheme(Text target, int fontSize, Color color)
        {
            if (target == null)
            {
                return;
            }

            target.fontSize = fontSize;
            target.color = color;
        }
    }
}

#pragma warning restore CS0649
