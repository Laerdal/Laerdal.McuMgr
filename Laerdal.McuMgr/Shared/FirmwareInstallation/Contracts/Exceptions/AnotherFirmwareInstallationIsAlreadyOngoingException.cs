using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class AnotherFirmwareInstallationIsAlreadyOngoingException : FirmwareInstallationErroredOutException
    {
        public AnotherFirmwareInstallationIsAlreadyOngoingException(string nativeErrorMessage, EFirmwareInstallerFatalErrorType fatalErrorType, EGlobalErrorCode eaGlobalErrorCode)
            : base(
                errorMessage: $"Another firmware installation is already ongoing: {nativeErrorMessage}",
                fatalErrorType: fatalErrorType,
                globalErrorCode: eaGlobalErrorCode
            )
        {
        }
    }
}