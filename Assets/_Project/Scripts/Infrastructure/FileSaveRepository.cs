using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DemonLord.Application;
using DemonLord.Domain;

namespace DemonLord.Infrastructure
{
    public sealed class FileSaveRepository : ISaveRepository
    {
        private const string SavesDirectoryName = "Saves";
        private const string PrimarySaveFileName = "save.json";
        private const string TemporarySaveFileName = "save.tmp";
        private const string BackupSaveFileName = "save.bak";

        private readonly string savesRootPath;
        private readonly ISaveJsonSerializer serializer;
        private readonly ISaveMigrationPipeline migrationPipeline;

        public FileSaveRepository(
            string persistentDataPath,
            ISaveJsonSerializer serializer,
            ISaveMigrationPipeline migrationPipeline)
        {
            if (string.IsNullOrWhiteSpace(persistentDataPath))
            {
                throw new ArgumentException("A persistent data path is required.", nameof(persistentDataPath));
            }

            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.migrationPipeline = migrationPipeline ?? throw new ArgumentNullException(nameof(migrationPipeline));
            savesRootPath = Path.Combine(persistentDataPath, SavesDirectoryName);
        }

        public IReadOnlyList<SaveSlotSummary> ListSlots()
        {
            List<SaveSlotSummary> summaries = new List<SaveSlotSummary>(SaveSlotId.AllValues.Count);
            foreach (string slotValue in SaveSlotId.AllValues)
            {
                SaveSlotId.TryCreate(slotValue, out SaveSlotId slotId);
                SaveReadResult readResult = Load(slotId);
                summaries.Add(ToSummary(slotId, readResult));
            }

            return summaries;
        }

        public SaveReadResult Load(SaveSlotId slotId)
        {
            if (slotId == null)
            {
                throw new ArgumentNullException(nameof(slotId));
            }

            string primaryPath = GetSavePath(slotId, PrimarySaveFileName);
            string backupPath = GetSavePath(slotId, BackupSaveFileName);

            try
            {
                bool hasPrimary = File.Exists(primaryPath);
                bool hasBackup = File.Exists(backupPath);
                if (!hasPrimary && !hasBackup)
                {
                    return SaveReadResult.Failure(SaveReadStatus.Empty, "save_not_found", null);
                }

                SaveReadResult primaryResult = hasPrimary
                    ? ReadAndValidate(primaryPath, slotId)
                    : SaveReadResult.Failure(SaveReadStatus.Corrupt, "primary_save_missing", null);

                if (primaryResult.IsSuccess || primaryResult.Status == SaveReadStatus.Incompatible || !hasBackup)
                {
                    return primaryResult;
                }

                SaveReadResult backupResult = ReadAndValidate(backupPath, slotId);
                if (backupResult.IsSuccess)
                {
                    return SaveReadResult.Success(backupResult.Save, true);
                }

                return SaveReadResult.Failure(
                    primaryResult.Status == SaveReadStatus.IoFailure ? SaveReadStatus.IoFailure : SaveReadStatus.Corrupt,
                    "primary_and_backup_invalid",
                    CombineDiagnostics(primaryResult.DiagnosticMessage, backupResult.DiagnosticMessage));
            }
            catch (IOException exception)
            {
                return SaveReadResult.Failure(SaveReadStatus.IoFailure, "save_read_io_error", exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return SaveReadResult.Failure(SaveReadStatus.IoFailure, "save_read_access_denied", exception.Message);
            }
        }

        public SaveWriteResult Save(GameSave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            string temporaryPath = GetSavePath(save.SlotId, TemporarySaveFileName);
            try
            {
                Directory.CreateDirectory(GetSlotDirectoryPath(save.SlotId));
                SaveEnvelopeDto envelope = CreateEnvelope(save);
                WriteAllTextAndFlush(temporaryPath, serializer.SerializeEnvelope(envelope));

                SaveReadResult temporaryResult = ReadAndValidate(temporaryPath, save.SlotId);
                if (!temporaryResult.IsSuccess || temporaryResult.Save.SaveId != save.SaveId)
                {
                    return SaveWriteResult.Failure(
                        SaveWriteStatus.ValidationFailure,
                        "temporary_save_validation_failed",
                        temporaryResult.DiagnosticMessage);
                }

                ReplacePrimaryWithTemporary(save.SlotId, temporaryPath);
                return SaveWriteResult.Success();
            }
            catch (IOException exception)
            {
                return SaveWriteResult.Failure(SaveWriteStatus.IoFailure, "save_write_io_error", exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return SaveWriteResult.Failure(SaveWriteStatus.IoFailure, "save_write_access_denied", exception.Message);
            }
            catch (ArgumentException exception)
            {
                return SaveWriteResult.Failure(SaveWriteStatus.ValidationFailure, "save_write_validation_error", exception.Message);
            }
            finally
            {
                DeleteTemporaryFile(temporaryPath);
            }
        }

        public SaveWriteResult Delete(SaveSlotId slotId)
        {
            if (slotId == null)
            {
                throw new ArgumentNullException(nameof(slotId));
            }

            try
            {
                DeleteIfPresent(GetSavePath(slotId, PrimarySaveFileName));
                DeleteIfPresent(GetSavePath(slotId, BackupSaveFileName));
                DeleteIfPresent(GetSavePath(slotId, TemporarySaveFileName));
                return SaveWriteResult.Success();
            }
            catch (IOException exception)
            {
                return SaveWriteResult.Failure(SaveWriteStatus.IoFailure, "save_delete_io_error", exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return SaveWriteResult.Failure(SaveWriteStatus.IoFailure, "save_delete_access_denied", exception.Message);
            }
        }

        private SaveReadResult ReadAndValidate(string filePath, SaveSlotId expectedSlotId)
        {
            string envelopeJson = File.ReadAllText(filePath, Encoding.UTF8);
            if (!serializer.TryDeserializeEnvelope(envelopeJson, out SaveEnvelopeDto envelope, out string envelopeDiagnostic))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, "invalid_save_envelope_json", envelopeDiagnostic);
            }

            if (envelope.schemaVersion > SaveSchema.CurrentVersion)
            {
                return SaveReadResult.Failure(SaveReadStatus.Incompatible, "future_schema_version", null);
            }

            if (string.IsNullOrWhiteSpace(envelope.payloadJson)
                || !PayloadChecksum.Matches(envelope.payloadJson, envelope.payloadSha256))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, "payload_checksum_mismatch", null);
            }

            if (envelope.schemaVersion < SaveSchema.CurrentVersion)
            {
                if (!migrationPipeline.TryMigrate(
                        envelope,
                        SaveSchema.CurrentVersion,
                        out envelope,
                        out string migrationErrorCode))
                {
                    return SaveReadResult.Failure(SaveReadStatus.Incompatible, migrationErrorCode, null);
                }
            }

            if (envelope.schemaVersion != SaveSchema.CurrentVersion)
            {
                return SaveReadResult.Failure(SaveReadStatus.Incompatible, "migration_target_schema_mismatch", null);
            }

            if (string.IsNullOrWhiteSpace(envelope.payloadJson)
                || !PayloadChecksum.Matches(envelope.payloadJson, envelope.payloadSha256))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, "migrated_payload_checksum_mismatch", null);
            }

            if (!SaveSlotId.TryCreate(envelope.slotId, out SaveSlotId storedSlotId) || !storedSlotId.Equals(expectedSlotId))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, "save_slot_id_mismatch", null);
            }

            if (!serializer.TryDeserializePayload(envelope.payloadJson, out GameSavePayloadDto payload, out string payloadDiagnostic))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, "invalid_save_payload_json", payloadDiagnostic);
            }

            if (!TryParseUtc(envelope.createdAtUtc, out DateTime createdAtUtc)
                || !TryParseUtc(envelope.updatedAtUtc, out DateTime updatedAtUtc))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, "invalid_save_timestamp", null);
            }

            if (!GameSaveMapper.TryFromPayloadDto(
                    envelope,
                    payload,
                    createdAtUtc,
                    updatedAtUtc,
                    out GameSave save,
                    out string mapperErrorCode))
            {
                return SaveReadResult.Failure(SaveReadStatus.Corrupt, mapperErrorCode, null);
            }

            return SaveReadResult.Success(save, false);
        }

        private SaveEnvelopeDto CreateEnvelope(GameSave save)
        {
            string payloadJson = serializer.SerializePayload(GameSaveMapper.ToPayloadDto(save));
            return new SaveEnvelopeDto
            {
                schemaVersion = save.SchemaVersion,
                saveId = save.SaveId,
                slotId = save.SlotId.Value,
                createdAtUtc = save.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                updatedAtUtc = save.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                buildVersion = save.BuildVersion,
                payloadJson = payloadJson,
                payloadSha256 = PayloadChecksum.ComputeSha256(payloadJson),
            };
        }

        private void ReplacePrimaryWithTemporary(SaveSlotId slotId, string temporaryPath)
        {
            string primaryPath = GetSavePath(slotId, PrimarySaveFileName);
            if (!File.Exists(primaryPath))
            {
                File.Move(temporaryPath, primaryPath);
                return;
            }

            SaveReadResult existingPrimary = ReadAndValidate(primaryPath, slotId);
            string backupPath = existingPrimary.IsSuccess ? GetSavePath(slotId, BackupSaveFileName) : null;
            File.Replace(temporaryPath, primaryPath, backupPath);
        }

        private SaveSlotSummary ToSummary(SaveSlotId slotId, SaveReadResult result)
        {
            if (result.IsSuccess)
            {
                return new SaveSlotSummary(
                    slotId,
                    SaveSlotState.Valid,
                    result.Save.Profile.ProfileName,
                    result.Save.Profile.DifficultyId.Value,
                    result.Save.UpdatedAtUtc,
                    result.RecoveredFromBackup,
                    null,
                    result.Save.PlayTimeSeconds,
                    result.Save.Progress.EntryId,
                    result.Save.Progress.CheckpointId);
            }

            if (result.Status == SaveReadStatus.Empty)
            {
                return SaveSlotSummary.Empty(slotId);
            }

            SaveSlotState state = result.Status == SaveReadStatus.Incompatible
                ? SaveSlotState.Incompatible
                : SaveSlotState.Corrupt;
            return new SaveSlotSummary(slotId, state, null, null, null, false, result.ErrorCode);
        }

        private string GetSlotDirectoryPath(SaveSlotId slotId)
        {
            return Path.Combine(savesRootPath, slotId.Value);
        }

        private string GetSavePath(SaveSlotId slotId, string fileName)
        {
            return Path.Combine(GetSlotDirectoryPath(slotId), fileName);
        }

        private static bool TryParseUtc(string value, out DateTime utcValue)
        {
            if (!DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsedValue)
                || parsedValue.Kind != DateTimeKind.Utc)
            {
                utcValue = default;
                return false;
            }

            utcValue = parsedValue;
            return true;
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

        private static string CombineDiagnostics(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first))
            {
                return second;
            }

            if (string.IsNullOrWhiteSpace(second))
            {
                return first;
            }

            return first + " | " + second;
        }

        private static void DeleteTemporaryFile(string temporaryPath)
        {
            try
            {
                DeleteIfPresent(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
