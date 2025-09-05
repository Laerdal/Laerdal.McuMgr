// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.Runtime.InteropServices;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string Resource { get; init; }
        public string ErrorMessage { get; init; }
        public EGlobalErrorCode GlobalErrorCode { get; init; }

        public FatalErrorOccurredEventArgs(string resource, string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            Resource = resource;
            ErrorMessage = errorMessage;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
