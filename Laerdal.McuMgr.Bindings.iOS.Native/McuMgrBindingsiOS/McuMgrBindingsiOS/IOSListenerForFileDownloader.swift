import Foundation

@objc
public protocol IOSListenerForFileDownloader {
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String, _ resource: String)
    func fatalErrorOccurredAdvertisement(_ resource: String, _ errorMessage: String)

    func cancelledAdvertisement()
    func stateChangedAdvertisement(_ resource: String, _ oldState: EIOSFileDownloaderState, _ newState: EIOSFileDownloaderState)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func downloadCompletedAdvertisement(_ resource: String, _ data: [UInt8])
    func fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_ progressPercentage: Int, _ averageThroughput: Float32)
}
