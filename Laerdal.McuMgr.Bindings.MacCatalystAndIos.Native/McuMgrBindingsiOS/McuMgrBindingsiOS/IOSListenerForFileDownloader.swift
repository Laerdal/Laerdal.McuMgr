import Foundation

@objc
public protocol IOSListenerForFileDownloader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String, _ resource: String?)
    func fatalErrorOccurredAdvertisement(_ resourceId: String?, _ errorMessage: String, _ globalErrorCode: Int)

    func cancelledAdvertisement()

    func stateChangedAdvertisement(_ resourceId: String?, _ oldState: EIOSFileDownloaderState, _ newState: EIOSFileDownloaderState)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func downloadCompletedAdvertisement(_ resourceId: String?, _ data: [UInt8])
    func fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_ resourceId: String?, _ progressPercentage: Int, _ averageThroughput: Float32, _ totalAverageThroughputInKbps: Float32)
}
