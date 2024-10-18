import Foundation

@objc
public protocol IOSListenerForFileUploader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String, _ resource: String)
    func fatalErrorOccurredAdvertisement(_ resource: String, _ errorMessage: String, _ errorCode: Int)

    func cancelledAdvertisement(_ reason: String)
    func cancellingAdvertisement(_ reason: String)

    func stateChangedAdvertisement(_ resource: String, _ oldState: EIOSFileUploaderState, _ newState: EIOSFileUploaderState)
    func fileUploadedAdvertisement(_ resource: String)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(_ progressPercentage: Int, _ averageThroughput: Float32)
}
