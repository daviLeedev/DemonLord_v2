using System;
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
        [SerializeField] private DialogueTheme theme;

        public event Action PresentationDisabled;

        public bool IsPresentationAvailable => panelRoot != null
            && panelRoot.activeInHierarchy
            && canvasGroup != null
            && isActiveAndEnabled;

        private void Awake()
        {
            ApplyTheme();
            Hide();
        }

        private void OnDisable()
        {
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

            if (bodyLabel != null)
            {
                bodyLabel.text = line.Text;
            }

            if (hintLabel != null)
            {
                hintLabel.text = "F / Enter 계속   Esc 닫기";
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
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
