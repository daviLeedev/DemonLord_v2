using System;

namespace DemonLord.Domain
{
    public enum SaveReadStatus
    {
        Success,
        Empty,
        Corrupt,
        Incompatible,
        IoFailure,
    }

    public sealed class SaveReadResult
    {
        private SaveReadResult(SaveReadStatus status, GameSave save, bool recoveredFromBackup, string errorCode, string diagnosticMessage)
        {
            Status = status;
            Save = save;
            RecoveredFromBackup = recoveredFromBackup;
            ErrorCode = errorCode;
            DiagnosticMessage = diagnosticMessage;
        }

        public SaveReadStatus Status { get; }

        public GameSave Save { get; }

        public bool RecoveredFromBackup { get; }

        public string ErrorCode { get; }

        public string DiagnosticMessage { get; }

        public bool IsSuccess => Status == SaveReadStatus.Success && Save != null;

        public static SaveReadResult Success(GameSave save, bool recoveredFromBackup)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            return new SaveReadResult(SaveReadStatus.Success, save, recoveredFromBackup, null, null);
        }

        public static SaveReadResult Failure(SaveReadStatus status, string errorCode, string diagnosticMessage)
        {
            if (status == SaveReadStatus.Success)
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            return new SaveReadResult(status, null, false, errorCode, diagnosticMessage);
        }
    }
}
