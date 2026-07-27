using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Infrastructure
{
    public sealed class UnityGameSettingsRuntimeApplier : IGameSettingsRuntimeApplier
    {
        public void Apply(GameSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            QualitySettings.vSyncCount = settings.VSyncEnabled ? 1 : 0;
            QualitySettings.SetQualityLevel(ToQualityIndex(settings.QualityPreset), true);
            Screen.fullScreenMode = ToUnityMode(settings.DisplayMode);
            Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight, Screen.fullScreenMode);
        }

        private static int ToQualityIndex(QualityPresetId qualityPreset)
        {
            int requested = qualityPreset == QualityPresetId.Low ? 0 : qualityPreset == QualityPresetId.Medium ? 1 : 2;
            return Mathf.Clamp(requested, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
        }

        private static FullScreenMode ToUnityMode(DisplayModeId displayMode)
        {
            switch (displayMode)
            {
                case DisplayModeId.Windowed:
                    return FullScreenMode.Windowed;
                case DisplayModeId.ExclusiveFullScreen:
                    return FullScreenMode.ExclusiveFullScreen;
                default:
                    return FullScreenMode.FullScreenWindow;
            }
        }
    }
}
