import Foundation

@objc
public class IOSFirmwareListDownloaderResult: NSObject {

    @objc public let verdict: EIOSFirmwareListDownloaderVerdict
    @objc public let jsonData: String
    @objc public let possibleErrorMessage: String

    @objc
    public init(verdict: EIOSFirmwareListDownloaderVerdict, jsonData: String, possibleErrorMessage: String) {
        self.verdict = verdict
        self.jsonData = jsonData
        self.possibleErrorMessage = possibleErrorMessage
    }

    public static func forError(verdict: EIOSFirmwareListDownloaderVerdict, possibleErrorMessage: String) -> IOSFirmwareListDownloaderResult {
        return IOSFirmwareListDownloaderResult(verdict: verdict, jsonData: "", possibleErrorMessage: possibleErrorMessage);
    }

    public static func forSuccess(jsonData: String) -> IOSFirmwareListDownloaderResult {
        return IOSFirmwareListDownloaderResult(verdict: .success, jsonData: jsonData, possibleErrorMessage: "");
    }
}
