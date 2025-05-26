// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; init; }
        public string RemoteFilePath { get; init; }

        public EGlobalErrorCode GlobalErrorCode { get; }

        public FatalErrorOccurredEventArgs(string remoteFilePath, string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            ErrorMessage = errorMessage;
            RemoteFilePath = remoteFilePath;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
