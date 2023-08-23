using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums
{
    [Flags]
    public enum EFirmwareInstallationVerdict //this must mirror the java enum values of E[Android|iOS]FirmwareInstallationVerdict
    {
        Success = 0,
        FailedInvalidDataFile = 0b0001, // 1
        FailedInvalidSettings = 0b0011, // 3
        FailedDeploymentError = 0b0101, // 5
        FailedInstallationAlreadyInProgress = 0b1001, // 9 
    }
}