@objc
public enum EIOSFirmwareInstallationVerdict: Int { //@formatter:off
    case                                       success = 0
    case                  failedGivenFirmwareUnhealthy = 1
    case                         failedInvalidSettings = 3
    case    failedInstallationInitializationErroredOut = 5
    case           failedInstallationAlreadyInProgress = 9
}
