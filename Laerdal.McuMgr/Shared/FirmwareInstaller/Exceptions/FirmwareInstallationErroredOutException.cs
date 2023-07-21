using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Exceptions
{
    public class FirmwareInstallationErroredOutException : Exception
    {
        public FirmwareInstallationErroredOutException(string errorMessage) : base($"An error occurred while performing the firmware installation: '{errorMessage}'")
        {
        }
    }
}