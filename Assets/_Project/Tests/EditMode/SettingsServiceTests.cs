using System;
using System.IO;
using DemonLord.Application;
using DemonLord.Domain;
using DemonLord.Infrastructure;
using NUnit.Framework;

namespace DemonLord.Tests.EditMode
{
    public sealed class SettingsServiceTests
    {
        private string temporaryDataPath;

        [SetUp]
        public void SetUp()
        {
            temporaryDataPath = Path.Combine(Path.GetTempPath(), "DemonLord_v2_SettingsTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDataPath))
            {
                Directory.Delete(temporaryDataPath, true);
            }
        }

        [Test]
        public void GameSettings_NormalizesUnsafeValues()
        {
            GameSettings settings = new GameSettings(
                -2f,
                5f,
                float.NaN,
                DisplayModeId.Windowed,
                777,
                333,
                false,
                QualityPresetId.Low,
                4f,
                true,
                true,
                true);

            Assert.That(settings.MasterVolume, Is.Zero);
            Assert.That(settings.BgmVolume, Is.EqualTo(1f));
            Assert.That(settings.SfxVolume, Is.EqualTo(1f));
            Assert.That(settings.ResolutionWidth, Is.EqualTo(1920));
            Assert.That(settings.ResolutionHeight, Is.EqualTo(1080));
            Assert.That(settings.UiScale, Is.EqualTo(GameSettings.MaximumUiScale));
        }

        [Test]
        public void FileSettingsRepository_RoundTripsAndKeepsSettingsSeparateFromSaves()
        {
            FileSettingsRepository repository = new FileSettingsRepository(temporaryDataPath);
            GameSettings original = GameSettings.Default.With(masterVolume: 0.4f, reduceTransitions: true, resolutionWidth: 1280, resolutionHeight: 720);

            SettingsWriteResult writeResult = repository.Save(original);
            SettingsReadResult readResult = repository.Load();

            Assert.That(writeResult.IsSuccess, Is.True, writeResult.DiagnosticMessage);
            Assert.That(readResult.IsSuccess, Is.True, readResult.DiagnosticMessage);
            Assert.That(readResult.Settings.SemanticallyEquals(original), Is.True);
            Assert.That(File.Exists(Path.Combine(temporaryDataPath, "Settings", "settings.json")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(temporaryDataPath, "Saves")), Is.False);
        }

        [Test]
        public void FileSettingsRepository_ReportsCorruptSettings()
        {
            FileSettingsRepository repository = new FileSettingsRepository(temporaryDataPath);
            string settingsDirectory = Path.Combine(temporaryDataPath, "Settings");
            Directory.CreateDirectory(settingsDirectory);
            File.WriteAllText(Path.Combine(settingsDirectory, "settings.json"), "{ invalid json");

            SettingsReadResult result = repository.Load();

            Assert.That(result.Status, Is.EqualTo(SettingsReadStatus.Corrupt));
        }

        [Test]
        public void SettingsService_CancelRestoresPersistedRuntimeSettings()
        {
            FakeSettingsRepository repository = new FakeSettingsRepository(GameSettings.Default);
            FakeRuntimeApplier applier = new FakeRuntimeApplier();
            SettingsService service = new SettingsService(repository, applier);
            service.LoadAndApply();
            service.BeginEdit();

            GameSettings preview = service.Working.With(masterVolume: 0.1f, reduceFlashes: true);
            service.SetWorking(preview);
            service.CancelEdit();

            Assert.That(applier.LastApplied.SemanticallyEquals(GameSettings.Default), Is.True);
            Assert.That(service.Working.SemanticallyEquals(GameSettings.Default), Is.True);
        }

        [Test]
        public void SettingsService_ResetAndSavePersistsWorkingCopy()
        {
            FakeSettingsRepository repository = new FakeSettingsRepository(GameSettings.Default.With(masterVolume: 0.2f));
            FakeRuntimeApplier applier = new FakeRuntimeApplier();
            SettingsService service = new SettingsService(repository, applier);
            service.LoadAndApply();
            service.BeginEdit();
            service.ResetWorking();

            SettingsWriteResult result = service.SaveWorking();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(repository.LastSaved.SemanticallyEquals(GameSettings.Default), Is.True);
            Assert.That(service.Persisted.SemanticallyEquals(GameSettings.Default), Is.True);
        }

        [Test]
        public void SettingsService_SaveFailureKeepsPersistedAndCancelRestoresItAtRuntime()
        {
            GameSettings persisted = GameSettings.Default.With(masterVolume: 0.35f, uiScale: 0.95f);
            FakeSettingsRepository repository = new FakeSettingsRepository(
                persisted,
                SettingsWriteResult.Failure("settings_write_failed", "test failure"));
            FakeRuntimeApplier applier = new FakeRuntimeApplier();
            SettingsService service = new SettingsService(repository, applier);
            service.LoadAndApply();
            service.BeginEdit();
            service.SetWorking(persisted.With(masterVolume: 0.85f, uiScale: 1.1f));

            SettingsWriteResult result = service.SaveWorking();
            service.CancelEdit();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(service.Persisted.SemanticallyEquals(persisted), Is.True);
            Assert.That(applier.LastApplied.SemanticallyEquals(persisted), Is.True);
        }

        private sealed class FakeRuntimeApplier : IGameSettingsRuntimeApplier
        {
            public GameSettings LastApplied { get; private set; }

            public void Apply(GameSettings settings)
            {
                LastApplied = settings;
            }
        }

        private sealed class FakeSettingsRepository : ISettingsRepository
        {
            private readonly GameSettings initial;
            private readonly SettingsWriteResult saveResult;

            public FakeSettingsRepository(GameSettings initial, SettingsWriteResult saveResult = null)
            {
                this.initial = initial;
                this.saveResult = saveResult ?? SettingsWriteResult.Success();
            }

            public GameSettings LastSaved { get; private set; }

            public SettingsReadResult Load()
            {
                return SettingsReadResult.Success(initial, false);
            }

            public SettingsWriteResult Save(GameSettings settings)
            {
                LastSaved = settings;
                return saveResult;
            }
        }
    }
}
