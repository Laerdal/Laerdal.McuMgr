using System;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums
{
    [Flags]
    public enum EFirmwareInstallationVerdict //@formatter:off   this must mirror the java enum values of E[Android|iOS]FirmwareInstallationVerdict
    {
                                           Success = 0,
                                            Failed = 0b00001, // 1   basic failure-flag

                      FailedGivenFirmwareUnhealthy = 0b00011, // 3
                             FailedInvalidSettings = 0b00101, // 5
        FailedInstallationInitializationErroredOut = 0b01001, // 9
               FailedInstallationAlreadyInProgress = 0b10001, // 17 
    }
}