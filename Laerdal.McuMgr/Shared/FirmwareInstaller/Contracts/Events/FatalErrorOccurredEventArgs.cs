// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct FatalErrorOccurredEventArgs : IMcuMgrEventArgs
    {
        public string ErrorMessage { get; }
        public EFirmwareInstallationState State { get; } //the state in which the error occurred
        public EFirmwareInstallerFatalErrorType FatalErrorType { get; }

        public FatalErrorOccurredEventArgs(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage)
        {
            State = state;
            ErrorMessage = errorMessage;
            FatalErrorType = fatalErrorType;
        }
    }
}
