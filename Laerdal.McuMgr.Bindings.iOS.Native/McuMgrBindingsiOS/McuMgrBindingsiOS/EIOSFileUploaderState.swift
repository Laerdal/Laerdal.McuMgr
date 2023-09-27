@objc
public enum EIOSFileUploaderState: Int {
    case none = 0
    case idle = 1
    case uploading = 2
    case paused = 3
    case complete = 4
    case cancelled = 5
    case error = 6
    case cancelling = 7 //when a cancellation is requested
}
