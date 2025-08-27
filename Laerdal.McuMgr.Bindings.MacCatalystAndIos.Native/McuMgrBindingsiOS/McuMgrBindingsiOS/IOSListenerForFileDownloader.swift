import Foundation

@objc
public protocol IOSListenerForFileDownloader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String, _ remoteFilePathSanitized: String?)
    func fatalErrorOccurredAdvertisement(_ remoteFilePathSanitized: String?, _ errorMessage: String, _ globalErrorCode: Int)

    func cancelledAdvertisement(_ reason: String?)
    func cancellingAdvertisement(_ reason: String?)

    func stateChangedAdvertisement(_ remoteFilePathSanitized: String?, _ oldState: EIOSFileDownloaderState, _ newState: EIOSFileDownloaderState)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func fileDownloadStartedAdvertisement(_ remoteFilePathSanitized: String?, _ totalBytesToBeUploaded: Int)
    func fileDownloadCompletedAdvertisement(_ remoteFilePathSanitized: String?, _ data: [UInt8]?)
    func fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_ remoteFilePathSanitized: String?, _ progressPercentage: Int, _ averageThroughput: Float32, _ totalAverageThroughputInKbps: Float32)
}
