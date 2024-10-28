// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; }
        public string ErrorMessage { get; }
        public EGlobalErrorCode GlobalErrorCode { get; }

        public FatalErrorOccurredEventArgs(string resource, string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            Resource = resource;
            ErrorMessage = errorMessage;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
