// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; init; }
        public EGlobalErrorCode GlobalErrorCode { get; init; }
        public EFirmwareInstallationState State { get; init; } //the state in which the error occurred
        public EFirmwareInstallerFatalErrorType FatalErrorType { get; init; }

        public FatalErrorOccurredEventArgs(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, EGlobalErrorCode globalErrorCode)
        {
            State = state;
            ErrorMessage = errorMessage;
            FatalErrorType = fatalErrorType;
            GlobalErrorCode = globalErrorCode;
        }
    }
}
