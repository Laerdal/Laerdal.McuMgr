// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public class FirmwareInstallationConfirmationStageTimeoutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationConfirmationStageTimeoutException(int? estimatedSwapTimeInMilliseconds, EGlobalErrorCode eaGlobalErrorCode)
            : base(
                errorMessage: $"Device didn't confirm the new firmware within the given timeframe of {estimatedSwapTimeInMilliseconds} milliseconds. The new firmware will only last for just one power-cycle of the device.",
                globalErrorCode: eaGlobalErrorCode
            )
        {
        }
    }
}