using System;

namespace DemonLord.Application
{
    public enum SaveWriteStatus
    {
        Success,
        ValidationFailure,
        IoFailure,
    }

    public sealed class SaveWriteResult
    {
        private SaveWriteResult(SaveWriteStatus status, string errorCode, string diagnosticMessage)
        {
            Status = status;
            ErrorCode = errorCode;
            DiagnosticMessage = diagnosticMessage;
        }

        public SaveWriteStatus Status { get; }

        public string ErrorCode { get; }

        public string DiagnosticMessage { get; }

        public bool IsSuccess => Status == SaveWriteStatus.Success;

        public static SaveWriteResult Success()
        {
            return new SaveWriteResult(SaveWriteStatus.Success, null, null);
        }

        public static SaveWriteResult Failure(SaveWriteStatus status, string errorCode, string diagnosticMessage)
        {
            if (status == SaveWriteStatus.Success)
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            return new SaveWriteResult(status, errorCode, diagnosticMessage);
        }
    }
}
