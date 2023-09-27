@objc
public enum EIOSFirmwareInstallationVerdict: Int {
    case success = 0
    case failedInvalidFirmware = 1
    case failedInvalidSettings = 3
    case failedDeploymentError = 5
    case failedInstallationAlreadyInProgress = 9
}
