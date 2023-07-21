namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public class FirmwareInstallationErroredOutImageSwapTimeoutException : FirmwareInstallationErroredOutException
    {
        public FirmwareInstallationErroredOutImageSwapTimeoutException()
            : base("Device didn't confirm the new firmware within the given timeframe")
        {
        }
    }
}