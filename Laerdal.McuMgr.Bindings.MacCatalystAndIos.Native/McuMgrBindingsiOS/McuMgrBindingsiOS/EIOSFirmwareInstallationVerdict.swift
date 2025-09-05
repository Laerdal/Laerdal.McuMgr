@objc
public enum EIOSFirmwareInstallationVerdict: Int { //@formatter:off    must be aligned with the respective verdict-enums in android and the csharp lib
    case                                       success = 0
    //case                                      failed = 1    // 0000001   basic failure-flag
    case                  failedGivenFirmwareUnhealthy = 3    // 0000011
    case                         failedInvalidSettings = 5    // 0000101
    case    failedInstallationInitializationErroredOut = 9    // 0001001
    case           failedInstallationAlreadyInProgress = 17   // 0010001
}
