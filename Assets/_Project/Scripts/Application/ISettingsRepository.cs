using DemonLord.Domain;

namespace DemonLord.Application
{
    public enum SettingsReadStatus
    {
        Success,
        Missing,
        Corrupt,
        IoFailure,
    }

    public sealed class SettingsReadResult
    {
        private SettingsReadResult(SettingsReadStatus status, GameSettings settings, bool recoveredFromBackup, string errorCode, string diagnosticMessage)
        {
            Status = status;
            Settings = settings;
            RecoveredFromBackup = recoveredFromBackup;
            ErrorCode = errorCode;
            DiagnosticMessage = diagnosticMessage;
        }

        public SettingsReadStatus Status { get; }

        public GameSettings Settings { get; }

        public bool RecoveredFromBackup { get; }

        public string ErrorCode { get; }

        public string DiagnosticMessage { get; }

        public bool IsSuccess => Status == SettingsReadStatus.Success && Settings != null;

        public static SettingsReadResult Success(GameSettings settings, bool recoveredFromBackup)
        {
            return new SettingsReadResult(SettingsReadStatus.Success, settings, recoveredFromBackup, null, null);
        }

        public static SettingsReadResult Failure(SettingsReadStatus status, string errorCode, string diagnosticMessage)
        {
            return new SettingsReadResult(status, null, false, errorCode, diagnosticMessage);
        }
    }

    public sealed class SettingsWriteResult
    {
        private SettingsWriteResult(bool success, string errorCode, string diagnosticMessage)
        {
            IsSuccess = success;
            ErrorCode = errorCode;
            DiagnosticMessage = diagnosticMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorCode { get; }

        public string DiagnosticMessage { get; }

        public static SettingsWriteResult Success()
        {
            return new SettingsWriteResult(true, null, null);
        }

        public static SettingsWriteResult Failure(string errorCode, string diagnosticMessage)
        {
            return new SettingsWriteResult(false, errorCode, diagnosticMessage);
        }
    }

    public interface ISettingsRepository
    {
        SettingsReadResult Load();

        SettingsWriteResult Save(GameSettings settings);
    }
}
