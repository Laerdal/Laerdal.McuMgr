@objc
public enum EIOSFirmwareInstallationState: Int {
    case none = 0
    case idle = 1
    case validating = 2
    case uploading = 3
    case paused = 4
    case testing = 5
    case resetting = 6
    case confirming = 7
    case complete = 8
    case cancelling = 9
    case cancelled = 10
    case error = 11
}