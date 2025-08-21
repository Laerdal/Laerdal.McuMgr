import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileDownloadX)
public class IOSFileDownloader: NSObject {

    private var _listener: IOSListenerForFileDownloader!
    private var _transporter: McuMgrBleTransport!
    private var _cbPeripheral: CBPeripheral!
    private var _fileSystemManager: FileSystemManager!

    private var _currentState: EIOSFileDownloaderState = .none
    private var _currentBusyState: Bool = false

    private var _lastFatalErrorMessage: String = ""
    
    private var _remoteFilePathSanitized: String = ""

    private var _lastBytesSent: Int = 0
    private var _lastBytesSentTimestamp: Date? = nil
    private var _downloadStartTimestamp: Date? = nil

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

        if !tryInvalidateCachedInfrastructure() { //order
            return false
        }

        _cbPeripheral = cbPeripheral //order
        return true
    }

    @objc
    public func tryInvalidateCachedInfrastructure() -> Bool {
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

        resetState() //order
        disposeFilesystemManager() //00 vital hack
        ensureTransportIsInitializedExactlyOnce(initialMtuSize) //order
        ensureFilesystemManagerIsInitializedExactlyOnce() //order

        setState(.idle) //order

        let verdict: EIOSFileDownloadingInitializationVerdict = ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                let success = _fileSystemManager?.download(name: _remoteFilePathSanitized, delegate: self) ?? false //order
                if !success {
                    onError("[IOSFD.BD.050] Failed to commence file-downloading (check logs for details)")

                    return .failedErrorUponCommencing
                }

                return .success

            } catch let ex {
                onError("[IOSFD.BD.060] Failed to commence file-downloading", ex)

                return .failedErrorUponCommencing
            }
        })

        return verdict

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
    }

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    @objc
    public func pause() {
        if (_fileSystemManager == nil) {
            return
        }

        ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                if (_fileSystemManager == nil) {
                    return
                }

                _fileSystemManager?.pauseTransfer()
                
                setState(.paused)
                setBusyState(false)
            } catch let ex {
                onError("[IOSFD.PAUSE.050] Failed to pause file-downloading", ex)
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
    }

    @objc
    public func resume() {
        if (_fileSystemManager == nil) {
            return
        }

        ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                if (_fileSystemManager == nil) {
                    return
                }

                _fileSystemManager?.continueTransfer()

                setState(.downloading)
                setBusyState(true)
            } catch let ex {
                onError("[IOSFD.RESUME.050] Failed to resume file-downloading", ex)
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
    }

    @objc
    public func cancel() {
        if (_fileSystemManager == nil) {
            return
        }

        ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                if (_fileSystemManager == nil) {
                    return
                }

                _fileSystemManager?.cancelTransfer() //order

                setState(.cancelling) //order
            } catch let ex {
                onError("[IOSFD.CANCEL.050] Failed to cancel file-downloading", ex)
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
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

    private func resetState() {
        _downloadStartTimestamp = nil

        _lastBytesSent = -1
        _lastBytesSentTimestamp = nil

        setState(.none)
        setBusyState(false)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_remoteFilePathSanitized, 0, 0, 0)
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
        let properMtu = initialMtuSize < 0 //                    -1=laerdal-mtu-default, 0=mtu-autoconfigured-by-nordic-libs, 1-and-above=user-mtu-custom-value
            ? Constants.DefaultMtuForFileDownloads //            at the time of this writing the mtu doesnt play a major role whwn downloading
            : initialMtuSize //                                  (it is mostly for when we are uploading) but we are applying it just in case

        _transporter = _transporter == nil
                ? McuMgrBleTransport(_cbPeripheral)
                : _transporter

        if properMtu > 0 {
            _transporter.mtu = properMtu

            logMessageAdvertisement("[IOSFD.ETIIEO.010] applied explicit initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
        } else {
            logMessageAdvertisement("[IOSFD.ETIIEO.020] using pre-set initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
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
        setBusyState(false) //                         order
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
            _ resourceId: String?,
            _ progressPercentage: Int,
            _ averageThroughput: Float32,
            _ totalAverageThroughputInKbps: Float32
    ) {
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, progressPercentage, averageThroughput, totalAverageThroughputInKbps)
        }
    }

    //@objc   dont
    private func fileDownloadStartedAdvertisement(_ resourceId: String?) {
        _listener.fileDownloadStartedAdvertisement(resourceId)
    }

    //@objc   dont
    private func fileDownloadCompletedAdvertisement(_ resourceId: String?, _ data: [UInt8]) {
        _listener.fileDownloadCompletedAdvertisement(resourceId, data)
    }

    private func setBusyState(_ newBusyState: Bool) {
        if (_currentBusyState == newBusyState) {
            return
        }

        busyStateChangedAdvertisement(newBusyState)
    }

    private func setState(_ newState: EIOSFileDownloaderState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order
        _currentState = newState //order
        stateChangedAdvertisement(oldState, newState) //order

        if (oldState == .idle && newState == .downloading) //00
        {
            fileDownloadStartedAdvertisement(_remoteFilePathSanitized)
        }
        else if (oldState == .downloading && newState == .complete) //00
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_remoteFilePathSanitized, 100, 0, 0)
        }

        //00 trivial hotfix to deal with the fact that the file-download progress% doesn't fill up to 100%
    }
}

extension IOSFileDownloader: FileDownloadDelegate {
    public func downloadProgressDidChange(bytesDownloaded bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(.downloading)
        setBusyState(true)

        let downloadProgressPercentage = (bytesSent * 100) / fileSize
        let currentThroughputInKbps = calculateCurrentThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)
        let totalAverageThroughputInKbps = calculateTotalAverageThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)

        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_remoteFilePathSanitized, downloadProgressPercentage, currentThroughputInKbps, totalAverageThroughputInKbps)
    }

    public func downloadDidFail(with error: Error) {
        onError(error.localizedDescription, error)

        setBusyState(false)
    }

    public func downloadDidCancel() {
        setState(.cancelled)
        setBusyState(false)
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_remoteFilePathSanitized, 0, 0, 0)
        cancelledAdvertisement()
    }

    public func download(of name: String, didFinish data: Data) {
        setState(.complete)
        fileDownloadCompletedAdvertisement(_remoteFilePathSanitized, [UInt8](data))
        setBusyState(false)
    }

    private func calculateCurrentThroughputInKbps(bytesSent: Int, timestamp: Date) -> Float32 {
        if (_lastBytesSentTimestamp == nil) {
            _lastBytesSent = bytesSent
            _lastBytesSentTimestamp = timestamp
            return 0
        }

        let intervalInSeconds = Float32(timestamp.timeIntervalSince(_lastBytesSentTimestamp!))
        if (intervalInSeconds == 0) { //almost impossible to happen but just in case
            _lastBytesSent = bytesSent
            _lastBytesSentTimestamp = timestamp
            return 0
        }

        let currentThroughputInKbps = Float32(bytesSent - _lastBytesSent) / (intervalInSeconds * 1024) //order

        _lastBytesSent = bytesSent //order
        _lastBytesSentTimestamp = timestamp //order

        return currentThroughputInKbps
    }

    private func calculateTotalAverageThroughputInKbps(bytesSent: Int, timestamp: Date) -> Float32 {
        if (_downloadStartTimestamp == nil) {
            _downloadStartTimestamp = timestamp
            return 0
        }

        let secondsSinceDownloadStart = Float32(timestamp.timeIntervalSince(_downloadStartTimestamp!))
        if (secondsSinceDownloadStart == 0) { //should be impossible but just in case
            return 0
        }

        return Float32(bytesSent) / (secondsSinceDownloadStart * 1024)
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
