// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public sealed class FirmwareInstallationImageSwappingTimedOutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationImageSwappingTimedOutException(int? estimatedSwapTimeInMilliseconds, EFirmwareInstallerFatalErrorType fatalErrorType, EGlobalErrorCode eaGlobalErrorCode)
            : base(
                errorMessage: $"Device didn't confirm the new firmware within the given timeframe of {estimatedSwapTimeInMilliseconds} milliseconds. The new firmware will only last for just one power-cycle of the device.",
                fatalErrorType: fatalErrorType,
                globalErrorCode: eaGlobalErrorCode
            )
        {
        }
    }
}