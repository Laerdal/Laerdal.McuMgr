@objc
public enum EIOSFileUploadingInitializationVerdict: Int {
    case success = 0
    case failedInvalidSettings = 1
    case failedInvalidData = 2
    case failedOtherUploadAlreadyInProgress = 3
}
