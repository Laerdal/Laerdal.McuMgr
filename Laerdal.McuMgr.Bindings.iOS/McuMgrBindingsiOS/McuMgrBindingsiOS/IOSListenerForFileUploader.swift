import Foundation

@objc
public protocol IOSListenerForFileUploader {
    func logMessageAdvertisement(_ remoteFilePath: String, _ message: String, _ category: String, _ level: String)
    func fatalErrorOccurredAdvertisement(_ remoteFilePath: String, _ errorMessage: String)

    func cancelledAdvertisement(_ remoteFilePath: String)
    func stateChangedAdvertisement(_ remoteFilePath: String, _ oldState: EIOSFileUploaderState, _ newState: EIOSFileUploaderState)
    func busyStateChangedAdvertisement(_ remoteFilePath: String, _ busyNotIdle: Bool)
    func fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(_ remoteFilePath: String, _ progressPercentage: Int, _ averageThroughput: Float32)
}
