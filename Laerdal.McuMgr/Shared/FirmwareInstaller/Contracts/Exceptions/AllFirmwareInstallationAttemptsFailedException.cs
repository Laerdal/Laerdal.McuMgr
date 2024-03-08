using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class AllFirmwareInstallationAttemptsFailedException : FirmwareInstallationErroredOutException, IFirmwareInstallationException
    {
        public AllFirmwareInstallationAttemptsFailedException(int triesCount, Exception innerException = null)
            : base($"Failed to install firmware after trying {triesCount} time(s)", innerException)
        {
        }
        
        public AllFirmwareInstallationAttemptsFailedException(string errorMessage)
            : base($"An error occurred while installing the firmware: '{errorMessage}'")
        {
        }
        
        public AllFirmwareInstallationAttemptsFailedException(string errorMessage, Exception innerException)
            : base($"An error occurred while installing the firmware: '{errorMessage}'", innerException)
        {
        }
    }
}
