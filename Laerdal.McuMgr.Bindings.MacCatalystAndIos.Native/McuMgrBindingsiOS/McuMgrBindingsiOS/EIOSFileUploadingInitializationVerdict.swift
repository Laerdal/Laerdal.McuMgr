@objc
public enum EIOSFileUploadingInitializationVerdict: Int {
    case success = 0
    case failedInvalidData = 1
    case failedInvalidSettings = 2
    case failedErrorUponCommencing = 3
    case failedOtherUploadAlreadyInProgress = 4
}
