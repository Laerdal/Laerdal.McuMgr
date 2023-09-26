// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationConfirmationStageTimeoutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationConfirmationStageTimeoutException(int? estimatedSwapTimeInMilliseconds)
            : base($"Device didn't confirm the new firmware within the given timeframe of {estimatedSwapTimeInMilliseconds} milliseconds. The new firmware will only last for just one power-cycle of the device.")
        {
        }
    }
}