import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileDownloadX)
public class IOSFileDownloader: NSObject {

    private var _minimumNativeLogLevel: McuMgrLogLevel = .error

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

    private var _cancellationReason: String = ""

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
    public func nativeDispose() {
        _ = tryInvalidateCachedInfrastructure() //doesnt throw
    }

    @objc
    public func tryDisconnect() -> Bool {
        do
        {
            _transporter?.close()
            //_transporter = nil //dont
            return true
        }
        catch let ex
        {
            logInBg("[IOSFD.DC.010] Failed to disconnect: '\(ex.localizedDescription)", McuMgrLogLevel.warning)
            return false
        }
    }

    @objc
    public func tryInvalidateCachedInfrastructure() -> Bool {
        let success1 = tryDisposeFilesystemManager() // order
        let success2 = tryDisposeTransport() //         order

        return success1 && success2
    }

    @objc
    public func beginDownload(_ remoteFilePath: String, _ minimumNativeLogLevelNumeric: Int, _ initialMtuSize: Int) -> EIOSFileDownloadingInitializationVerdict {
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

        _minimumNativeLogLevel = McuMgrLogLevelHelpers.translateLogLevel(minimumNativeLogLevelNumeric)

        resetState() //order
        _ = tryDisposeFilesystemManager() //00 vital hack
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
    public func trySetMinimumNativeLogLevel(_ minimumNativeLogLevelNumeric: Int) -> Bool {
        _minimumNativeLogLevel = McuMgrLogLevelHelpers.translateLogLevel(minimumNativeLogLevelNumeric)

        return true
    }

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    @objc
    public func tryPause() -> Bool {
        if (_currentState == .paused) { //order
            logInBg("[IOSFD.TPS.010] Ignoring 'pause' request in the native layer because we're already in 'paused' state anyway", McuMgrLogLevel.info)
            return true // already paused which is ok
        }

        if (_currentState != .downloading) { //order
            logInBg("[IOSFD.TPS.020] Ignoring 'pause' request in the native layer because we're not in a 'downloading' state to begin with", McuMgrLogLevel.info)
            return false
        }

        return ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                if (_fileSystemManager == nil) {
                    logInBg("[IOSFD.TPS.030] Ignoring 'pause' request in the native layer because the file-system-manager has been trashed", McuMgrLogLevel.info)
                    return false
                }

                logInBg("[IOSFD.TPS.040] Pausing downloading ...", McuMgrLogLevel.verbose)

                _fileSystemManager?.pauseTransfer()
                
                setState(.paused)
                setBusyState(false)

                return true
            } catch let ex {
                onError("[IOSFD.PAUSE.050] Failed to pause file-downloading", ex)
                return false
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
    }

    @objc
    public func tryResume() -> Bool {
        if _currentState == .downloading || _currentState == .resuming { //order
            logInBg("[IOSFD.TR.010] Ignoring 'resume' request because we're already in 'downloading/resuming' state anyway", McuMgrLogLevel.info)
            return true //already downloading which is ok
        }

        if (_currentState != .paused) { //order
            logInBg("[IOSFD.TR.020] Ignoring 'resume' request because we're not in a 'paused' state to begin with", McuMgrLogLevel.info)
            return false
        }

        if (_fileSystemManager == nil) { //order
            return false
        }

        return ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                if (_fileSystemManager == nil) {
                    logInBg("[IOSFD.TR.030] Ignoring 'resume' request because the file-system-manager is null", McuMgrLogLevel.info)
                    return false
                }

                logInBg("[IOSFD.TR.040] Resuming downloading ...", McuMgrLogLevel.verbose)

                _fileSystemManager?.continueTransfer()

                setState(.resuming)
                setBusyState(true)

                return true
            } catch let ex {
                onError("[IOSFD.RESUME.050] Failed to resume file-downloading", ex)
                return false
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
    }

    @objc
    public func tryCancel(_ reason: String = "") -> Bool {
        _cancellationReason = reason
        DispatchQueue.global(qos: .background).async { self.cancellingAdvertisement(reason) } // order
        setState(.cancelling) //                                                                 order

        if (_fileSystemManager == nil) { //order
            return false
        }

        return ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10  order
            do {
                _fileSystemManager?.cancelTransfer() //order
                return true

            } catch let ex {
                onError("[IOSFD.CANCEL.050] Failed to cancel file-downloading", ex)
                return false
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
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

            logInBg("[IOSFD.ETIIEO.010] applied explicit initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogLevel.info)
        } else {
            logInBg("[IOSFD.ETIIEO.020] using pre-set initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogLevel.info)
        }
    }

    private func tryDisposeTransport() -> Bool {
        if (_transporter == nil) {
            return true //already disconnected
        }

        do
        {
            _transporter?.close()
            _transporter = nil
            return true
        }
        catch let ex
        {
            logInBg("[IOSFD.TDT.010] Failed to dispose the transport: '\(ex.localizedDescription)'", McuMgrLogLevel.warning)
            return false
        }
    }

    private func tryDisposeFilesystemManager() -> Bool {
        //_fileSystemManager?.cancelTransfer()  dont
        _fileSystemManager = nil

        return true
    }

    //@objc   dont
    private func onError(_ errorMessage: String, _ error: Error? = nil) {
        _lastFatalErrorMessage = errorMessage //       order

        setState(.error) //                            order
        setBusyState(false) //                         order

        let remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.fatalErrorOccurredAdvertisement( //  order
                    remoteFilePathSanitizedSnapshot,
                    errorMessage,
                    McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error)
            )
        }
    }

    //@objc   dont
    private static let DefaultLogCategory = "FileUploader";

    private func logInBg(_ message: String, _ level: McuMgrLogLevel, _ category: String = DefaultLogCategory) {
        if (level < _minimumNativeLogLevel) {
            return
        }

        let remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.logMessageAdvertisement(message, category, level.name, remoteFilePathSanitizedSnapshot)
        }
    }

    private func log(_ message: String, _ level: McuMgrLogLevel) {
        if (level < _minimumNativeLogLevel) {
            return
        }

        self._listener.logMessageAdvertisement(message, IOSFileDownloader.DefaultLogCategory, level.name, _remoteFilePathSanitized)
    }

    //@objc   dont
    private func cancellingAdvertisement(_ reason: String?) {
        _listener.cancellingAdvertisement(reason)
    }

    //@objc   dont
    private func cancelledAdvertisement(_ reason: String?) {
        _listener.cancelledAdvertisement(reason)
    }

    //@objc   dont
    private func busyStateChangedAdvertisement(_ busyNotIdle: Bool) {
        _listener.busyStateChangedAdvertisement(busyNotIdle)
    }

    //@objc   dont
    private func fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
            _ resourceId: String?,
            _ progressPercentage: Int,
            _ averageThroughput: Float32,
            _ totalAverageThroughputInKbps: Float32
    ) {
        _listener.fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, progressPercentage, averageThroughput, totalAverageThroughputInKbps)
    }

    private func setBusyState(_ newBusyState: Bool) {
        if (_currentBusyState == newBusyState) {
            return
        }

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self.busyStateChangedAdvertisement(newBusyState)
        }
    }

    private func setState(_ newState: EIOSFileDownloaderState, _ data: [UInt8]? = nil) {
        return setState(newState, 0, data)
    }

    private func setState(_ newState: EIOSFileDownloaderState, _ totalBytesToBeUploaded: Int = 0, _ data: [UInt8]? = nil) {
        if (_currentState == .paused && newState == .downloading) {
            return; // after pausing we might still get a quick DOWNLOADING update from the native-layer - we must ignore it
        }

        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //                                    order
        _currentState = newState //                                        order
        let remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized //  order

        DispatchQueue.global(qos: .background).async {
            self._listener.stateChangedAdvertisement(remoteFilePathSanitizedSnapshot, oldState, newState, totalBytesToBeUploaded, data)
        }
    }
}

extension IOSFileDownloader: FileDownloadDelegate {
    public func downloadProgressDidChange(bytesDownloaded bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(.downloading)
        setBusyState(true)

        let remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            let downloadProgressPercentage = (bytesSent * 100) / fileSize
            let currentThroughputInKbps = self.calculateCurrentThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)
            let totalAverageThroughputInKbps = self.calculateTotalAverageThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)

            self.fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
                    remoteFilePathSanitizedSnapshot,
                    downloadProgressPercentage,
                    currentThroughputInKbps,
                    totalAverageThroughputInKbps
            )
        }
    }

    public func downloadDidFail(with error: Error) {
        onError(error.localizedDescription, error)
        setBusyState(false)
    }

    public func downloadDidCancel() {
        setState(.cancelled) //                                                                                    order
        DispatchQueue.global(qos: .background).async { self.cancelledAdvertisement(self._cancellationReason) } //  order
        setBusyState(false) //                                                                                     order
    }

    public func download(of name: String, didFinish data: Data) {
        setState(.complete, 0, [UInt8](data))
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
        logInBg(msg, level, category.rawValue)
    }
}
