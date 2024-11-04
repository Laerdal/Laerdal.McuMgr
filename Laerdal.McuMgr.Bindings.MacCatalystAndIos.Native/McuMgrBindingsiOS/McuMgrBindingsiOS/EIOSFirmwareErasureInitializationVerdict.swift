@objc
public enum EIOSFirmwareErasureInitializationVerdict: Int {
    case success = 0
    case failedErrorUponCommencing = 1
    case failedOtherErasureAlreadyInProgress = 2
}
