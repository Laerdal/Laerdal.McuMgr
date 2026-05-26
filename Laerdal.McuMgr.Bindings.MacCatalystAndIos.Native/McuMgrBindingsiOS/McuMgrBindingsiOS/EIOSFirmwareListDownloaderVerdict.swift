@objc
public enum EIOSFirmwareListDownloaderVerdict: Int { //this must mirror the enum values of E[Android|iOS]FirmwareListDownloaderVerdict
    case success = 0
    case failedInvalidData = 1
    case failedInvalidSettings = 2
}
