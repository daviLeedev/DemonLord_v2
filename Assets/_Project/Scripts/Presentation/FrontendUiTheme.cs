using UnityEngine;
using UnityEngine.UI;

namespace DemonLord.Presentation
{
    public enum FrontendTypography
    {
        Title,
        Subtitle,
        MenuPrimary,
        MenuSecondary,
        Body,
        Caption,
    }

    /// <summary>
    /// Keeps frontend typography and contrast rules in one place. A licensed Korean font placed at
    /// Resources/UI/Fonts/FrontendKorean is picked up automatically; the built-in font is a safe fallback.
    /// </summary>
    public sealed class FrontendUiTheme
    {
        private const string KoreanFontResourcePath = "UI/Fonts/FrontendKorean";

        public FrontendUiTheme()
        {
            Font = Resources.Load<Font>(KoreanFontResourcePath)
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public Font Font { get; }

        public Color Paper => ColorFromHex("F4EBDD");

        public Color Focus => ColorFromHex("8FD2FF");

        public Color Warning => ColorFromHex("E17A72");

        public Color MenuPrimary => ColorFromHex("FFF4D9");

        public Color MenuSecondary => ColorFromHex("BFD7F5");

        public Color Disabled => new Color(0.7f, 0.7f, 0.7f, 0.72f);

        public void Apply(Text text, FrontendTypography typography, Color color, TextAnchor alignment)
        {
            text.font = Font;
            text.fontSize = GetFontSize(typography);
            text.fontStyle = typography == FrontendTypography.MenuPrimary || typography == FrontendTypography.Title
                ? FontStyle.Bold
                : FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
        }

        public void AddContrast(Text text, bool strong)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, strong ? 0.98f : 0.78f);
            shadow.effectDistance = strong ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, strong ? 0.86f : 0.56f);
            outline.effectDistance = Vector2.one;
        }

        private static int GetFontSize(FrontendTypography typography)
        {
            switch (typography)
            {
                case FrontendTypography.Title:
                    return 42;
                case FrontendTypography.Subtitle:
                    return 28;
                case FrontendTypography.MenuPrimary:
                    return 24;
                case FrontendTypography.MenuSecondary:
                    return 18;
                case FrontendTypography.Body:
                    return 26;
                default:
                    return 19;
            }
        }

        private static Color ColorFromHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
