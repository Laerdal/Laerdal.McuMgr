@objc
public enum EIOSFileDownloadingInitializationVerdict: Int {
    case success = 0
    case failedInvalidSettings = 1
    case failedErrorUponCommencing = 2
    case failedDownloadAlreadyInProgress = 3
}
