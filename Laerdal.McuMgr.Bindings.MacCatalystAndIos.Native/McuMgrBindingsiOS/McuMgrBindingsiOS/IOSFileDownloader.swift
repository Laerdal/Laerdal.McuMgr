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
    public func beginDownload(_ remoteFilePath: String, _ initialMtuSize: Int) -> EIOSFileDownloadingInitializationVerdict {

        if !isCold() { //keep first   if another download is already in progress we bail out
            onError("[IOSFD.BD.010] Another download is already in progress")

            return .failedDownloadAlreadyInProgress
        }

        _remoteFilePathSanitized = remoteFilePath.trimmingCharacters(in: .whitespacesAndNewlines)
        if _remoteFilePathSanitized.isEmpty {
            onError("[IOSFD.BD.020] Target-file provided is dud!")

            return .failedInvalidSettings
        }

        if _remoteFilePathSanitized.hasSuffix("/") {
            onError("[IOSFD.BD.030] Target-file points to a directory instead of a file")

            return .failedInvalidSettings
        }

        if !_remoteFilePathSanitized.hasPrefix("/") {
            onError("[IOSFD.BD.040] Target-path is not absolute!")

            return .failedInvalidSettings
        }

        resetUploadState() //order
        disposeFilesystemManager() //00 vital hack
        ensureTransportIsInitializedExactlyOnce(initialMtuSize) //order
        ensureFilesystemManagerIsInitializedExactlyOnce() //order

        let success = _fileSystemManager.download(name: _remoteFilePathSanitized, delegate: self)
        if !success {
            onError("[IOSFD.BD.050] Failed to commence file-Downloading (check logs for details)")

            return .failedErrorUponCommencing
        }

        return .success
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
        return _currentState == .idle || isCold();
    }

    private func isCold() -> Bool {
        return _currentState == .none
                || _currentState == .error
                || _currentState == .complete
                || _currentState == .cancelled
    }

    private func resetUploadState() {
        _lastBytesSend = -1
        _lastBytesSendTimestamp = nil

        setState(.idle)
        busyStateChangedAdvertisement(true)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
    }

    private func ensureFilesystemManagerIsInitializedExactlyOnce() {
        if _fileSystemManager != nil { //already initialized?
            return
        }

        _fileSystemManager = FileSystemManager(transport: _transporter) //00
        _fileSystemManager.logDelegate = self //00

        //00  this doesnt throw an error   the log-delegate aspect is implemented in the extension below via IOSFileDownloader: McuMgrLogDelegate
    }

    private func ensureTransportIsInitializedExactlyOnce(_ initialMtuSize: Int) {
        let properMtu = initialMtuSize <= 0 //                      at the time of this writing the mtu doesnt play a major role whwn downloading
            ? Constants.DefaultMtuForFileUploadsAndDownloads //     (it is mostly for when we are uploading) but we are applying it just in case
            : initialMtuSize

        _transporter = _transporter == nil
                ? McuMgrBleTransport(_cbPeripheral)
                : _transporter

        if properMtu > 0 {
            _transporter.mtu = properMtu

            logMessageAdvertisement("[IOSFD.ETIIEO.010] applied explicit initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
        }
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
        _lastFatalErrorMessage = errorMessage //       order
        setState(.error) //                            order
        _listener.fatalErrorOccurredAdvertisement( //  order
                _remoteFilePathSanitized,
                errorMessage,
                McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error)
        )
    }

    //@objc   dont
    private func logMessageAdvertisement(_ message: String, _ category: String, _ level: String) {
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.logMessageAdvertisement(message, category, level, self._remoteFilePathSanitized)
        }
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
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput)
        }
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

        if (oldState == .downloading && newState == .complete) //00
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0)
        }

        //00 trivial hotfix to deal with the fact that the file-download progress% doesn't fill up to 100%
    }
}

extension IOSFileDownloader: FileDownloadDelegate {
    public func downloadProgressDidChange(bytesDownloaded bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(.downloading)
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let DownloadProgressPercentage = (bytesSent * 100) / fileSize
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(DownloadProgressPercentage, throughputKilobytesPerSecond)
    }

    public func downloadDidFail(with error: Error) {
        onError(error.localizedDescription, error)

        busyStateChangedAdvertisement(false)
    }

    public func downloadDidCancel() {
        setState(.cancelled)
        busyStateChangedAdvertisement(false)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
        cancelledAdvertisement()
    }

    public func download(of name: String, didFinish data: Data) {
        setState(.complete)
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
