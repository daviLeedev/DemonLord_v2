using System;
using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    public enum DialogueSpeakerSide
    {
        Player = 0,
        Partner = 1,
        Narration = 2,
    }

    [Serializable]
    public sealed class DialogueParticipant
    {
        [SerializeField] private string speakerId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Sprite portrait;

        public string SpeakerId => speakerId ?? string.Empty;

        public string DisplayName => displayName ?? string.Empty;

        public Sprite Portrait => portrait;

        public void Configure(string configuredSpeakerId, string configuredDisplayName, Sprite configuredPortrait)
        {
            speakerId = configuredSpeakerId ?? string.Empty;
            displayName = configuredDisplayName ?? string.Empty;
            portrait = configuredPortrait;
        }
    }

    [Serializable]
    public sealed class DialogueLine
    {
        [SerializeField] private DialogueSpeakerSide speakerSide;
        [SerializeField, TextArea(2, 4)] private string text = string.Empty;
        [SerializeField] private string portraitVariantId = "neutral";

        public DialogueSpeakerSide SpeakerSide => speakerSide;

        public string Text => text ?? string.Empty;

        public string PortraitVariantId => portraitVariantId ?? string.Empty;

        public void Configure(DialogueSpeakerSide configuredSide, string configuredText, string configuredPortraitVariantId = "neutral")
        {
            speakerSide = configuredSide;
            text = configuredText ?? string.Empty;
            portraitVariantId = configuredPortraitVariantId ?? string.Empty;
        }
    }

    [CreateAssetMenu(menuName = "DemonLord/Exploration/Dialogue Sequence", fileName = "DialogueSequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [SerializeField] private DialogueParticipant player = new DialogueParticipant();
        [SerializeField] private DialogueParticipant partner = new DialogueParticipant();
        [SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

        public int LineCount => lines == null ? 0 : lines.Length;

        public DialogueParticipant Player => player;

        public DialogueParticipant Partner => partner;

        public DialogueLine GetLine(int index)
        {
            return lines != null && index >= 0 && index < lines.Length ? lines[index] : null;
        }

        public DialogueParticipant GetParticipant(DialogueSpeakerSide side)
        {
            return side == DialogueSpeakerSide.Player ? player : partner;
        }

        public bool IsValid()
        {
            if (lines == null || lines.Length == 0 || player == null || partner == null)
            {
                return false;
            }

            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index] == null || string.IsNullOrWhiteSpace(lines[index].Text))
                {
                    return false;
                }
            }

            return true;
        }

        public void Configure(
            DialogueParticipant configuredPlayer,
            DialogueParticipant configuredPartner,
            DialogueLine[] configuredLines)
        {
            player = configuredPlayer ?? new DialogueParticipant();
            partner = configuredPartner ?? new DialogueParticipant();
            lines = configuredLines == null ? Array.Empty<DialogueLine>() : (DialogueLine[])configuredLines.Clone();
        }
    }
}
