// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationImageSwapTimeoutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationImageSwapTimeoutException()
            : base("Device didn't confirm the new firmware within the given timeframe")
        {
        }
    }
}