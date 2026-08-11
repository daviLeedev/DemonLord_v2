using UnityEngine;

namespace DemonLord.Presentation.Exploration
{
    [CreateAssetMenu(menuName = "DemonLord/Exploration/Dialogue Theme", fileName = "DialogueTheme")]
    public sealed class DialogueTheme : ScriptableObject
    {
        [SerializeField] private Font font;
        [SerializeField, Min(1)] private int bodyFontSize = 36;
        [SerializeField, Min(1)] private int nameFontSize = 30;
        [SerializeField] private Color bodyColor = new Color(0.95f, 0.92f, 0.84f, 1f);
        [SerializeField] private Color playerAccent = new Color(0.45f, 0.78f, 1f, 1f);
        [SerializeField] private Color partnerAccent = new Color(0.82f, 0.58f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float inactivePortraitAlpha = 0.55f;

        public Font Font => font;

        public int BodyFontSize => bodyFontSize;

        public int NameFontSize => nameFontSize;

        public Color BodyColor => bodyColor;

        public Color PlayerAccent => playerAccent;

        public Color PartnerAccent => partnerAccent;

        public float InactivePortraitAlpha => inactivePortraitAlpha;

        public void Configure(Font configuredFont)
        {
            font = configuredFont;
            bodyFontSize = 32;
            nameFontSize = 28;
            bodyColor = new Color(0.94f, 0.95f, 0.98f, 1f);
            playerAccent = new Color(0.40f, 0.78f, 0.91f, 1f);
            partnerAccent = new Color(0.78f, 0.66f, 0.96f, 1f);
            inactivePortraitAlpha = 0.62f;
        }
    }
}
