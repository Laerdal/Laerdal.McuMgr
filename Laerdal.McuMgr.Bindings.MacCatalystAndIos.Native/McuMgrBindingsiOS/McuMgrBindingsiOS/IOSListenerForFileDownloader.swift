import Foundation

@objc
public protocol IOSListenerForFileDownloader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String, _ resourceId: String?)
    func fatalErrorOccurredAdvertisement(_ resourceId: String?, _ errorMessage: String, _ globalErrorCode: Int)

    func cancelledAdvertisement(_ reason: String?)
    func cancellingAdvertisement(_ reason: String?)

    func stateChangedAdvertisement(_ resourceId: String?, _ oldState: EIOSFileDownloaderState, _ newState: EIOSFileDownloaderState)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func fileDownloadStartedAdvertisement(_ resourceId: String?, _ totalBytesToBeUploaded: Int)
    func fileDownloadCompletedAdvertisement(_ resourceId: String?, _ data: [UInt8]?)
    func fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_ resourceId: String?, _ progressPercentage: Int, _ averageThroughput: Float32, _ totalAverageThroughputInKbps: Float32)
}
