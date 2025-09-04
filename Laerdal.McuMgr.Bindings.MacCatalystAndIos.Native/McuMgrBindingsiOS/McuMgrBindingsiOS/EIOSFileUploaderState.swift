@objc
public enum EIOSFileUploaderState: Int { //@formatter:off
    case none        = 0
    case idle        = 1
    case uploading   = 2
    case paused      = 3
    case resuming    = 4
    case complete    = 5
    case cancelled   = 6
    case error       = 7
    case cancelling  = 8 //when a cancellation is requested
}
