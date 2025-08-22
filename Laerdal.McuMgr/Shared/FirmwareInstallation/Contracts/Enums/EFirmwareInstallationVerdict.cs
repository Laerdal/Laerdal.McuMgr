using System;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums
{
    [Flags]
    public enum EFirmwareInstallationVerdict //@formatter:off   this must mirror the java enum values of E[Android|iOS]FirmwareInstallationVerdict
    {
                                           Success = 0,

                      FailedGivenFirmwareUnhealthy = 0b0001, // 1
                             FailedInvalidSettings = 0b0011, // 3
        FailedInstallationInitializationErroredOut = 0b0101, // 5
               FailedInstallationAlreadyInProgress = 0b1001, // 9 
    }
}