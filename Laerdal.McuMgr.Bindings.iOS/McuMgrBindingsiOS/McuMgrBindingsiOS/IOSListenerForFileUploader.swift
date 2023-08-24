import Foundation

@objc
public protocol IOSListenerForFileUploader {
    func logMessageAdvertisement(_ resource: String, _ message: String, _ category: String, _ level: String)
    func fatalErrorOccurredAdvertisement(_ resource: String, _ errorMessage: String)

    func cancelledAdvertisement()
    func stateChangedAdvertisement(_ resource: String, _ oldState: EIOSFileUploaderState, _ newState: EIOSFileUploaderState)
    func uploadCompletedAdvertisement(_ resource: String)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(_ progressPercentage: Int, _ averageThroughput: Float32)
}
