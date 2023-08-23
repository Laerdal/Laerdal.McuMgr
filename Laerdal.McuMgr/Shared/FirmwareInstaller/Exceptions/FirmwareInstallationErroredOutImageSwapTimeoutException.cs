// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public class FirmwareInstallationErroredOutImageSwapTimeoutException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public FirmwareInstallationErroredOutImageSwapTimeoutException()
            : base("Device didn't confirm the new firmware within the given timeframe")
        {
        }
    }
}