@objc
public enum EIOSDeviceResetInitializationVerdict: Int {
    case success = 0
    case failedErrorUponCommencing = 1
    case failedOtherResetAlreadyInProgress = 2
}
