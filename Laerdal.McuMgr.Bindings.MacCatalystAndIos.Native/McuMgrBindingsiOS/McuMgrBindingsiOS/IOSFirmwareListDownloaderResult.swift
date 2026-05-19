@objc
public class IOSFirmwareListDownloaderResult: NSObject {

    @objc public let verdict: EIOSFirmwareListDownloaderVerdict
    @objc public let data: String

    @objc
    public init(verdict: EIOSFirmwareListDownloaderVerdict, data: String) {
        self.verdict = verdict
        self.data = data
    }
}

