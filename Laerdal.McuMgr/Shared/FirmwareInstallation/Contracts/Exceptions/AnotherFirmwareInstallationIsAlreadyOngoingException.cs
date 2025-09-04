using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class AnotherFirmwareInstallationIsAlreadyOngoingException : FirmwareInstallationErroredOutException
    {
        public AnotherFirmwareInstallationIsAlreadyOngoingException( //@formatter:off
            string                             nativeErrorMessage = "",
            EFirmwareInstallerFatalErrorType   fatalErrorType     = EFirmwareInstallerFatalErrorType.Generic,
            EGlobalErrorCode                   globalErrorCode    = EGlobalErrorCode.Generic
        ) //@formatter:on
            : base(
                errorMessage: ProperlyFormatErrorMessage(nativeErrorMessage),
                fatalErrorType: fatalErrorType,
                globalErrorCode: globalErrorCode
            )
        {
        }

        static private string ProperlyFormatErrorMessage(string nativeErrorMessage)
        {
            const string prefix = "Another firmware installation is already ongoing";
            return string.IsNullOrWhiteSpace(nativeErrorMessage)
                ? prefix
                : $"Another firmware installation is already ongoing: {nativeErrorMessage}";
        }
    }
}