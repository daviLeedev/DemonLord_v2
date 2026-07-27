using System;
using System.IO;
using System.Text;
using DemonLord.Application;
using DemonLord.Domain;
using UnityEngine;

namespace DemonLord.Infrastructure
{
    /// <summary>
    /// Atomic user settings store. It is deliberately separate from Saves so changing a preference
    /// can never modify a slot save.
    /// </summary>
    public sealed class FileSettingsRepository : ISettingsRepository
    {
        private const int CurrentSchemaVersion = 1;
        private const string SettingsDirectoryName = "Settings";
        private const string PrimaryFileName = "settings.json";
        private const string TemporaryFileName = "settings.tmp";
        private const string BackupFileName = "settings.bak";

        private readonly string settingsDirectoryPath;

        public FileSettingsRepository(string persistentDataPath)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath))
            {
                throw new ArgumentException("A persistent data path is required.", nameof(persistentDataPath));
            }

            settingsDirectoryPath = Path.Combine(persistentDataPath, SettingsDirectoryName);
        }

        public SettingsReadResult Load()
        {
            string primaryPath = GetPath(PrimaryFileName);
            string backupPath = GetPath(BackupFileName);
            try
            {
                bool primaryExists = File.Exists(primaryPath);
                bool backupExists = File.Exists(backupPath);
                if (!primaryExists && !backupExists)
                {
                    return SettingsReadResult.Failure(SettingsReadStatus.Missing, "settings_not_found", null);
                }

                SettingsReadResult primaryResult = primaryExists
                    ? Read(primaryPath)
                    : SettingsReadResult.Failure(SettingsReadStatus.Corrupt, "settings_primary_missing", null);
                if (primaryResult.IsSuccess)
                {
                    return primaryResult;
                }

                if (backupExists)
                {
                    SettingsReadResult backupResult = Read(backupPath);
                    if (backupResult.IsSuccess)
                    {
                        return SettingsReadResult.Success(backupResult.Settings, true);
                    }
                }

                return primaryResult;
            }
            catch (IOException exception)
            {
                return SettingsReadResult.Failure(SettingsReadStatus.IoFailure, "settings_read_io_error", exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return SettingsReadResult.Failure(SettingsReadStatus.IoFailure, "settings_read_access_denied", exception.Message);
            }
        }

        public SettingsWriteResult Save(GameSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            string temporaryPath = GetPath(TemporaryFileName);
            try
            {
                Directory.CreateDirectory(settingsDirectoryPath);
                string json = JsonUtility.ToJson(ToDto(settings), false);
                WriteAllTextAndFlush(temporaryPath, json);

                SettingsReadResult validation = Read(temporaryPath);
                if (!validation.IsSuccess || !validation.Settings.SemanticallyEquals(settings))
                {
                    return SettingsWriteResult.Failure("settings_temp_validation_failed", validation.DiagnosticMessage);
                }

                string primaryPath = GetPath(PrimaryFileName);
                string backupPath = GetPath(BackupFileName);
                if (File.Exists(primaryPath))
                {
                    File.Replace(temporaryPath, primaryPath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, primaryPath);
                }

                return SettingsWriteResult.Success();
            }
            catch (IOException exception)
            {
                return SettingsWriteResult.Failure("settings_write_io_error", exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return SettingsWriteResult.Failure("settings_write_access_denied", exception.Message);
            }
            finally
            {
                TryDelete(temporaryPath);
            }
        }

        private SettingsReadResult Read(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return SettingsReadResult.Failure(SettingsReadStatus.Corrupt, "settings_json_empty", null);
            }

            try
            {
                SettingsDto dto = JsonUtility.FromJson<SettingsDto>(json);
                if (dto == null || dto.schemaVersion != CurrentSchemaVersion)
                {
                    return SettingsReadResult.Failure(SettingsReadStatus.Corrupt, "settings_schema_invalid", null);
                }

                if (!Enum.IsDefined(typeof(DisplayModeId), dto.displayMode)
                    || !Enum.IsDefined(typeof(QualityPresetId), dto.qualityPreset))
                {
                    return SettingsReadResult.Failure(SettingsReadStatus.Corrupt, "settings_enum_invalid", null);
                }

                GameSettings settings = new GameSettings(
                    dto.masterVolume,
                    dto.bgmVolume,
                    dto.sfxVolume,
                    (DisplayModeId)dto.displayMode,
                    dto.resolutionWidth,
                    dto.resolutionHeight,
                    dto.vSyncEnabled,
                    (QualityPresetId)dto.qualityPreset,
                    dto.uiScale,
                    dto.reduceScreenShake,
                    dto.reduceFlashes,
                    dto.reduceTransitions);
                return SettingsReadResult.Success(settings, false);
            }
            catch (ArgumentException exception)
            {
                return SettingsReadResult.Failure(SettingsReadStatus.Corrupt, "settings_json_invalid", exception.Message);
            }
        }

        private string GetPath(string fileName)
        {
            return Path.Combine(settingsDirectoryPath, fileName);
        }

        private static SettingsDto ToDto(GameSettings settings)
        {
            return new SettingsDto
            {
                schemaVersion = CurrentSchemaVersion,
                masterVolume = settings.MasterVolume,
                bgmVolume = settings.BgmVolume,
                sfxVolume = settings.SfxVolume,
                displayMode = (int)settings.DisplayMode,
                resolutionWidth = settings.ResolutionWidth,
                resolutionHeight = settings.ResolutionHeight,
                vSyncEnabled = settings.VSyncEnabled,
                qualityPreset = (int)settings.QualityPreset,
                uiScale = settings.UiScale,
                reduceScreenShake = settings.ReduceScreenShake,
                reduceFlashes = settings.ReduceFlashes,
                reduceTransitions = settings.ReduceTransitions,
            };
        }

        private static void WriteAllTextAndFlush(string path, string contents)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(contents);
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [Serializable]
        private sealed class SettingsDto
        {
            public int schemaVersion;
            public float masterVolume;
            public float bgmVolume;
            public float sfxVolume;
            public int displayMode;
            public int resolutionWidth;
            public int resolutionHeight;
            public bool vSyncEnabled;
            public int qualityPreset;
            public float uiScale;
            public bool reduceScreenShake;
            public bool reduceFlashes;
            public bool reduceTransitions;
        }
    }
}
