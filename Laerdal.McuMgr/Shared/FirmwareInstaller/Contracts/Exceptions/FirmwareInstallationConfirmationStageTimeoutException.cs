// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationConfirmationStageTimeoutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationConfirmationStageTimeoutException()
            : base("Device didn't confirm the new firmware within the given timeframe")
        {
        }
    }
}