// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System.Runtime.InteropServices;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Events
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; init; }
        public EGlobalErrorCode GlobalErrorCode { get; init; }

        public FatalErrorOccurredEventArgs(string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            ErrorMessage = errorMessage;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
