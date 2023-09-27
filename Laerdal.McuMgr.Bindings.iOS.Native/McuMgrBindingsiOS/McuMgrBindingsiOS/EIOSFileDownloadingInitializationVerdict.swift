@objc
public enum EIOSFileDownloadingInitializationVerdict: Int {
    case success = 0
    case failedInvalidSettings = 1
    case failedDownloadAlreadyInProgress = 2
}
