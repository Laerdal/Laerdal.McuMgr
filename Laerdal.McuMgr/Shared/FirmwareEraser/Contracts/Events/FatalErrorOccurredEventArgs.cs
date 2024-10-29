// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; }
        public EGlobalErrorCode GlobalErrorCode { get; }

        public FatalErrorOccurredEventArgs(string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            ErrorMessage = errorMessage;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
