using System;
using DemonLord.Domain;

namespace DemonLord.Application
{
    public interface IGameSettingsRuntimeApplier
    {
        void Apply(GameSettings settings);
    }

    /// <summary>
    /// Owns the persisted and in-progress copies so Cancel can restore runtime values without using PlayerPrefs.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly ISettingsRepository repository;
        private readonly IGameSettingsRuntimeApplier runtimeApplier;

        public SettingsService(ISettingsRepository repository, IGameSettingsRuntimeApplier runtimeApplier)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.runtimeApplier = runtimeApplier ?? throw new ArgumentNullException(nameof(runtimeApplier));
            Persisted = GameSettings.Default;
            Working = Persisted;
        }

        public GameSettings Persisted { get; private set; }

        public GameSettings Working { get; private set; }

        public bool RecoveredFromInvalidFile { get; private set; }

        public string LoadNoticeCode { get; private set; }

        public void LoadAndApply()
        {
            SettingsReadResult result = repository.Load();
            if (result.IsSuccess)
            {
                Persisted = result.Settings;
                RecoveredFromInvalidFile = result.RecoveredFromBackup;
                LoadNoticeCode = result.RecoveredFromBackup ? "settings_recovered_from_backup" : null;
            }
            else
            {
                Persisted = GameSettings.Default;
                RecoveredFromInvalidFile = result.Status != SettingsReadStatus.Missing;
                LoadNoticeCode = RecoveredFromInvalidFile ? "settings_reset_to_default" : null;
            }

            Working = Persisted;
            runtimeApplier.Apply(Persisted);
        }

        public GameSettings BeginEdit()
        {
            Working = Persisted;
            return Working;
        }

        public void SetWorking(GameSettings settings)
        {
            Working = settings ?? throw new ArgumentNullException(nameof(settings));
            runtimeApplier.Apply(Working);
        }

        public void ResetWorking()
        {
            SetWorking(GameSettings.Default);
        }

        public SettingsWriteResult SaveWorking()
        {
            SettingsWriteResult result = repository.Save(Working);
            if (result.IsSuccess)
            {
                Persisted = Working;
            }

            return result;
        }

        public void CancelEdit()
        {
            Working = Persisted;
            runtimeApplier.Apply(Persisted);
        }
    }
}
