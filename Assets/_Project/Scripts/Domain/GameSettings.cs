using System;

namespace DemonLord.Domain
{
    public enum DisplayModeId
    {
        FullScreenWindow,
        Windowed,
        ExclusiveFullScreen,
    }

    public enum QualityPresetId
    {
        Low,
        Medium,
        High,
    }

    /// <summary>
    /// User-wide preferences. This data intentionally lives outside a save slot.
    /// </summary>
    public sealed class GameSettings
    {
        public const float MinimumUiScale = 0.9f;
        public const float MaximumUiScale = 1.1f;

        public GameSettings(
            float masterVolume,
            float bgmVolume,
            float sfxVolume,
            DisplayModeId displayMode,
            int resolutionWidth,
            int resolutionHeight,
            bool vSyncEnabled,
            QualityPresetId qualityPreset,
            float uiScale,
            bool reduceScreenShake,
            bool reduceFlashes,
            bool reduceTransitions)
        {
            MasterVolume = ClampNormalized(masterVolume);
            BgmVolume = ClampNormalized(bgmVolume);
            SfxVolume = ClampNormalized(sfxVolume);
            DisplayMode = Enum.IsDefined(typeof(DisplayModeId), displayMode)
                ? displayMode
                : DisplayModeId.FullScreenWindow;
            if (IsSupportedResolution(resolutionWidth, resolutionHeight))
            {
                ResolutionWidth = resolutionWidth;
                ResolutionHeight = resolutionHeight;
            }
            else
            {
                ResolutionWidth = 1920;
                ResolutionHeight = 1080;
            }
            VSyncEnabled = vSyncEnabled;
            QualityPreset = Enum.IsDefined(typeof(QualityPresetId), qualityPreset)
                ? qualityPreset
                : QualityPresetId.High;
            UiScale = ClampUiScale(uiScale);
            ReduceScreenShake = reduceScreenShake;
            ReduceFlashes = reduceFlashes;
            ReduceTransitions = reduceTransitions;
        }

        public float MasterVolume { get; }

        public float BgmVolume { get; }

        public float SfxVolume { get; }

        public DisplayModeId DisplayMode { get; }

        public int ResolutionWidth { get; }

        public int ResolutionHeight { get; }

        public bool VSyncEnabled { get; }

        public QualityPresetId QualityPreset { get; }

        public float UiScale { get; }

        public bool ReduceScreenShake { get; }

        public bool ReduceFlashes { get; }

        public bool ReduceTransitions { get; }

        public static GameSettings Default { get; } = new GameSettings(
            1f,
            0.75f,
            0.8f,
            DisplayModeId.FullScreenWindow,
            1920,
            1080,
            true,
            QualityPresetId.High,
            1f,
            false,
            false,
            false);

        public GameSettings With(
            float? masterVolume = null,
            float? bgmVolume = null,
            float? sfxVolume = null,
            DisplayModeId? displayMode = null,
            int? resolutionWidth = null,
            int? resolutionHeight = null,
            bool? vSyncEnabled = null,
            QualityPresetId? qualityPreset = null,
            float? uiScale = null,
            bool? reduceScreenShake = null,
            bool? reduceFlashes = null,
            bool? reduceTransitions = null)
        {
            return new GameSettings(
                masterVolume ?? MasterVolume,
                bgmVolume ?? BgmVolume,
                sfxVolume ?? SfxVolume,
                displayMode ?? DisplayMode,
                resolutionWidth ?? ResolutionWidth,
                resolutionHeight ?? ResolutionHeight,
                vSyncEnabled ?? VSyncEnabled,
                qualityPreset ?? QualityPreset,
                uiScale ?? UiScale,
                reduceScreenShake ?? ReduceScreenShake,
                reduceFlashes ?? ReduceFlashes,
                reduceTransitions ?? ReduceTransitions);
        }

        public bool SemanticallyEquals(GameSettings other)
        {
            return other != null
                && Math.Abs(MasterVolume - other.MasterVolume) < 0.001f
                && Math.Abs(BgmVolume - other.BgmVolume) < 0.001f
                && Math.Abs(SfxVolume - other.SfxVolume) < 0.001f
                && DisplayMode == other.DisplayMode
                && ResolutionWidth == other.ResolutionWidth
                && ResolutionHeight == other.ResolutionHeight
                && VSyncEnabled == other.VSyncEnabled
                && QualityPreset == other.QualityPreset
                && Math.Abs(UiScale - other.UiScale) < 0.001f
                && ReduceScreenShake == other.ReduceScreenShake
                && ReduceFlashes == other.ReduceFlashes
                && ReduceTransitions == other.ReduceTransitions;
        }

        private static float ClampNormalized(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float ClampUiScale(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            return Math.Max(MinimumUiScale, Math.Min(MaximumUiScale, value));
        }

        private static bool IsSupportedResolution(int width, int height)
        {
            return (width == 1280 && height == 720)
                || (width == 1920 && height == 1080)
                || (width == 2560 && height == 1440)
                || (width == 3440 && height == 1440);
        }
    }
}
