@objc
public enum EIOSFileDownloaderState: Int {
    case none = 0
    case idle = 1
    case downloading = 2
    case paused = 3
    case complete = 4
    case cancelled = 5
    case error = 6
    case cancelling = 7
}
