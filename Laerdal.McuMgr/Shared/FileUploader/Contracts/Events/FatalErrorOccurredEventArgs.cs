// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; }
        public string RemoteFilePath { get; }

        public EGlobalErrorCode GlobalErrorCode { get; }

        public FatalErrorOccurredEventArgs(string remoteFilePath, string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            ErrorMessage = errorMessage;
            RemoteFilePath = remoteFilePath;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
