import Foundation

@objc
public protocol IOSListenerForFileUploader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String, _ resourceId: String?)
    func fatalErrorOccurredAdvertisement(_ resourceId: String?, _ removeFilePath: String?, _ errorMessage: String, _ globalErrorCode: Int)

    func cancelledAdvertisement(_ reason: String?)
    func cancellingAdvertisement(_ reason: String?)

    func stateChangedAdvertisement(_ resourceId: String?, _ remoteFilePath: String?, _ oldState: EIOSFileUploaderState, _ newState: EIOSFileUploaderState, _ totalBytesToBeUploaded: Int)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
        _ resourceId: String?,
        _ remoteFilePath: String?,
        _ progressPercentage: Int,
        _ currentThroughputInKbps: Float32,
        _ averageThroughputInKbps: Float32
    )
}
