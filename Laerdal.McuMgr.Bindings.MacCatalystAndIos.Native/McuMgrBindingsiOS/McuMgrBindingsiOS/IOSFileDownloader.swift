import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileDownloadX)
public class IOSFileDownloader: NSObject {

    private var _listener: IOSListenerForFileDownloader!
    private var _transporter: McuMgrBleTransport!
    private var _currentState: EIOSFileDownloaderState
    private var _fileSystemManager: FileSystemManager!
    private var _lastFatalErrorMessage: String

    private var _lastBytesSend: Int = -1
    private var _lastBytesSendTimestamp: Date? = nil
    private var _remoteFilePathSanitized: String

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFileDownloader!) {
        _listener = listener
        _transporter = McuMgrBleTransport(cbPeripheral)
        _currentState = .none
        _lastFatalErrorMessage = ""
        _remoteFilePathSanitized = ""
    }

    @objc
    public func beginDownload(_ remoteFilePath: String) -> EIOSFileDownloadingInitializationVerdict {
        if _currentState != .none
                   && _currentState != .error
                   && _currentState != .complete
                   && _currentState != .cancelled { //if another download is already in progress we bail out
            return EIOSFileDownloadingInitializationVerdict.failedDownloadAlreadyInProgress
        }

        _lastBytesSend = -1
        _lastBytesSendTimestamp = nil

        if remoteFilePath.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            setState(EIOSFileDownloaderState.error)
            fatalErrorOccurredAdvertisement("", "Target-file provided is dud!")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        if remoteFilePath.hasSuffix("/") {
            setState(EIOSFileDownloaderState.error)
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, "Target-file points to a directory instead of a file")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        if !remoteFilePath.hasPrefix("/") {
            setState(EIOSFileDownloaderState.error)
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, "Target-path is not absolute!")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        _fileSystemManager = FileSystemManager(transport: _transporter) // the delegate aspect is implemented in the extension below
        _fileSystemManager.logDelegate = self

        setState(EIOSFileDownloaderState.idle)
        busyStateChangedAdvertisement(true)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)

        _remoteFilePathSanitized = remoteFilePath
        let success = _fileSystemManager.download(name: remoteFilePath, delegate: self)
        if !success {
            setState(EIOSFileDownloaderState.error)
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, "Failed to commence file-Downloading (check logs for details)")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        return EIOSFileDownloadingInitializationVerdict.success
    }

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    @objc
    public func pause() {
        _fileSystemManager?.pauseTransfer()

        setState(.paused)
    }

    @objc
    public func resume() {
        _fileSystemManager?.continueTransfer()

        setState(.downloading)
    }

    @objc
    public func cancel() {
        setState(.cancelling) //order

        _fileSystemManager?.cancelTransfer() //order
    }

    @objc
    public func disconnect() {
        _transporter?.close()
    }

    //@objc   dont
    private func fatalErrorOccurredAdvertisement(_ resource: String, _ errorMessage: String) {
        _lastFatalErrorMessage = errorMessage

        _listener.fatalErrorOccurredAdvertisement(resource, errorMessage)
    }

    //@objc   dont
    private func logMessageAdvertisement(_ message: String, _ category: String, _ level: String) {
        _listener.logMessageAdvertisement(message, category, level, _remoteFilePathSanitized)
    }

    //@objc   dont
    private func cancelledAdvertisement() {
        _listener.cancelledAdvertisement()
    }

    //@objc   dont
    private func busyStateChangedAdvertisement(_ busyNotIdle: Bool) {
        _listener.busyStateChangedAdvertisement(busyNotIdle)
    }

    //@objc   dont
    private func stateChangedAdvertisement(
            _ oldState: EIOSFileDownloaderState,
            _ newState: EIOSFileDownloaderState
    ) {
        _listener.stateChangedAdvertisement(_remoteFilePathSanitized, oldState, newState)
    }

    //@objc   dont
    private func fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
            _ progressPercentage: Int,
            _ averageThroughput: Float32
    ) {
        _listener.fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput)
    }
    
    //@objc   dont
    private func downloadCompletedAdvertisement(_ resource: String, _ data: [UInt8]) {
        _listener.downloadCompletedAdvertisement(resource, data)
    }

    private func setState(_ newState: EIOSFileDownloaderState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        stateChangedAdvertisement(oldState, newState) //order

        if (oldState == EIOSFileDownloaderState.downloading && newState == EIOSFileDownloaderState.complete) //00
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0)
        }

        //00 trivial hotfix to deal with the fact that the file-download progress% doesn't fill up to 100%
    }
}

extension IOSFileDownloader: FileDownloadDelegate {
    public func downloadProgressDidChange(bytesDownloaded bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(EIOSFileDownloaderState.downloading)
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let DownloadProgressPercentage = (bytesSent * 100) / fileSize
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(DownloadProgressPercentage, throughputKilobytesPerSecond)
    }

    public func downloadDidFail(with error: Error) {
        setState(EIOSFileDownloaderState.error)
        fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, error.localizedDescription)
        busyStateChangedAdvertisement(false)
    }

    public func downloadDidCancel() {
        setState(EIOSFileDownloaderState.cancelled)
        busyStateChangedAdvertisement(false)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
        cancelledAdvertisement()
    }

    public func download(of name: String, didFinish data: Data) {
        setState(EIOSFileDownloaderState.complete)
        downloadCompletedAdvertisement(_remoteFilePathSanitized, [UInt8](data))
        busyStateChangedAdvertisement(false)
    }

    private func calculateThroughput(bytesSent: Int, timestamp: Date) -> Float32 {
        if (_lastBytesSendTimestamp == nil) {
            _lastBytesSend = bytesSent
            _lastBytesSendTimestamp = timestamp
            return 0
        }

        let intervalInSeconds = Float32(timestamp.timeIntervalSince(_lastBytesSendTimestamp!).truncatingRemainder(dividingBy: 1))
        if (intervalInSeconds == 0) {
            _lastBytesSend = bytesSent
            _lastBytesSendTimestamp = timestamp
            return 0
        }

        let throughputKilobytesPerSecond = Float32(bytesSent - _lastBytesSend) / (intervalInSeconds * 1024)

        _lastBytesSend = bytesSent
        _lastBytesSendTimestamp = timestamp

        return throughputKilobytesPerSecond
    }
}

extension IOSFileDownloader: McuMgrLogDelegate {
    public func log(
            _ msg: String,
            ofCategory category: iOSMcuManagerLibrary.McuMgrLogCategory,
            atLevel level: iOSMcuManagerLibrary.McuMgrLogLevel
    ) {
        logMessageAdvertisement(
                msg,
                category.rawValue,
                level.name
        )
    }
}
