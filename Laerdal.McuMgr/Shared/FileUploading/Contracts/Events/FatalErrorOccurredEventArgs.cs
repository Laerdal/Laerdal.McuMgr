// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ResourceId { get; init; }
        public string ErrorMessage { get; init; }
        public string RemoteFilePath { get; init; }

        public EGlobalErrorCode GlobalErrorCode { get; }

        public FatalErrorOccurredEventArgs(string resourceId, string remoteFilePath, string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            ResourceId = resourceId;
            ErrorMessage = errorMessage;
            RemoteFilePath = remoteFilePath;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
