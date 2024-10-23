import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileDownloadX)
public class IOSFileDownloader: NSObject {

    private var _listener: IOSListenerForFileDownloader!
    private var _transporter: McuMgrBleTransport!
    private var _cbPeripheral: CBPeripheral!
    private var _fileSystemManager: FileSystemManager!

    private var _currentState: EIOSFileDownloaderState = .none
    private var _lastBytesSend: Int = -1
    private var _lastFatalErrorMessage: String = ""
    private var _lastBytesSendTimestamp: Date? = nil
    private var _remoteFilePathSanitized: String = ""

    @objc
    public init(_ listener: IOSListenerForFileDownloader!) {
        _listener = listener
    }

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFileDownloader!) {
        _listener = listener
        _cbPeripheral = cbPeripheral
    }

    @objc
    public func trySetBluetoothDevice(_ cbPeripheral: CBPeripheral!) -> Bool {
        if !isIdleOrCold() {
            return false
        }

        if !tryInvalidateCachedTransport() { //order
            return false
        }

        _cbPeripheral = cbPeripheral //order
        return true
    }

    @objc
    public func tryInvalidateCachedTransport() -> Bool {
        if _transporter == nil { //already scrapped
            return true
        }

        if !isIdleOrCold() { //if the upload is already in progress we bail out
            return false
        }

        disposeFilesystemManager() // order
        disposeTransport() //         order

        return true;
    }

    @objc
    public func beginDownload(_ remoteFilePath: String) -> EIOSFileDownloadingInitializationVerdict {

        if !isCold() { //keep first   if another download is already in progress we bail out
            onError("Another download is already in progress")

            return EIOSFileDownloadingInitializationVerdict.failedDownloadAlreadyInProgress
        }

        _remoteFilePathSanitized = remoteFilePath.trimmingCharacters(in: .whitespacesAndNewlines)
        if _remoteFilePathSanitized.isEmpty {
            onError("Target-file provided is dud!")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        if _remoteFilePathSanitized.hasSuffix("/") {
            onError("Target-file points to a directory instead of a file")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        if !_remoteFilePathSanitized.hasPrefix("/") {
            onError("Target-path is not absolute!")

            return EIOSFileDownloadingInitializationVerdict.failedInvalidSettings
        }

        resetUploadState() //order
        disposeFilesystemManager() //00 vital hack
        ensureTransportIsInitializedExactlyOnce() //order
        ensureFilesystemManagerIsInitializedExactlyOnce() //order

        let success = _fileSystemManager.download(name: _remoteFilePathSanitized, delegate: self)
        if !success {
            onError("Failed to commence file-Downloading (check logs for details)")

            return EIOSFileDownloadingInitializationVerdict.failedErrorUponCommencing
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

    private func isIdleOrCold() -> Bool {
        return _currentState == EIOSFileDownloaderState.idle || isCold();
    }

    private func isCold() -> Bool {
        return _currentState == EIOSFileDownloaderState.none
                || _currentState == EIOSFileDownloaderState.error
                || _currentState == EIOSFileDownloaderState.complete
                || _currentState == EIOSFileDownloaderState.cancelled
    }

    private func resetUploadState() {
        _lastBytesSend = -1
        _lastBytesSendTimestamp = nil

        setState(EIOSFileDownloaderState.idle)
        busyStateChangedAdvertisement(true)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
    }

    private func ensureFilesystemManagerIsInitializedExactlyOnce() {
        if _fileSystemManager != nil { //already initialized
            return
        }

        _fileSystemManager = FileSystemManager(transport: _transporter) //00
        _fileSystemManager.logDelegate = self //00

        //00  this doesnt throw an error   the log-delegate aspect is implemented in the extension below via IOSFileDownloader: McuMgrLogDelegate
    }

    private func ensureTransportIsInitializedExactlyOnce() {
        if _transporter != nil {
            return
        }

        _transporter = McuMgrBleTransport(_cbPeripheral)
    }

    private func disposeTransport() {
        _transporter?.close()
        _transporter = nil
    }

    private func disposeFilesystemManager() {
        //_fileSystemManager?.cancelTransfer()  dont
        _fileSystemManager = nil
    }

    //@objc   dont
    private func onError(_ errorMessage: String, _ error: Error? = nil) {
        setState(EIOSFileDownloaderState.error) //keep first

        _lastFatalErrorMessage = errorMessage

        let (errorCode, _) = deduceErrorCode(errorMessage)

        _listener.fatalErrorOccurredAdvertisement(
                _remoteFilePathSanitized,
                errorMessage,
                errorCode
        )
    }

    // unfortunately I couldnt figure out a way to deduce the error code from the error itself so I had to resort to string sniffing   ugly but it works
    private func deduceErrorCode(_ errorMessage: String) -> (Int, String?) {
        let (matchesArray, possibleError) = matches(for: " [(]\\d+[)][.]?$", in: errorMessage) // "UNKNOWN (1)."
        if possibleError != nil {
            return (-99, possibleError)
        }

        let errorCode = matchesArray.isEmpty
                ? -99
                : (Int(matchesArray[0].trimmingCharacters(in: .whitespaces).trimmingCharacters(in: ["(", ")", "."]).trimmingCharacters(in: .whitespaces)) ?? 0)

        return (errorCode, possibleError)
    }

    private func matches(for regex: String, in text: String) -> ([String], String?) { //00
        do {
            let regex = try NSRegularExpression(pattern: regex)
            let results = regex.matches(in: text, range: NSRange(text.startIndex..., in: text))

            return (
                    results.map {
                        String(text[Range($0.range, in: text)!])
                    },
                    nil
            )
        } catch let error {
            print("invalid regex: \(error.localizedDescription)")

            return ([], error.localizedDescription)
        }

        //00  https://stackoverflow.com/a/27880748/863651
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
        onError(error.localizedDescription, error)

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
