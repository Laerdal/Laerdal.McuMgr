import Foundation

@objc
public protocol IOSListenerForFileDownloader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String)
    func fatalErrorOccurredAdvertisement(_ errorMessage: String)

    func cancelledAdvertisement()
    func stateChangedAdvertisement(_ oldState: EIOSFileDownloaderState, _ newState: EIOSFileDownloaderState)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func downloadCompletedAdvertisement(_ data: [UInt8])
    func fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(_ progressPercentage: Int, _ averageThroughput: Float32)
}
