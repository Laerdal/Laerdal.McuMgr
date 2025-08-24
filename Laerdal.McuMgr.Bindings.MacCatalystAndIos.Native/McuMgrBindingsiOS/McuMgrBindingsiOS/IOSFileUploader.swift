import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileUploadX)
public class IOSFileUploader: NSObject {

    private let _listener: IOSListenerForFileUploader!
    private var _transporter: McuMgrBleTransport!
    private var _cbPeripheral: CBPeripheral!
    private var _fileSystemManager: FileSystemManager!

    private var _currentState: EIOSFileUploaderState = .none
    private var _currentBusyState: Bool = false

    private var _cancellationReason: String = ""
    private var _lastFatalErrorMessage: String = ""

    private var _lastBytesSent: Int = 0
    private var _uploadStartTimestamp: Date? = nil
    private var _lastBytesSentTimestamp: Date? = nil

    private var _resourceId: String = ""
    private var _remoteFilePathSanitized: String = ""

    @objc
    public init(_ listener: IOSListenerForFileUploader!) {
        _listener = listener
    }

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFileUploader!) {
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
        tryInvalidateCachedInfrastructure() //doesnt throw
    }

    @objc
    public func tryInvalidateCachedInfrastructure() -> Bool { // must be public
        var success1 = tryDisposeFilesystemManager() // order
        var success2 = tryDisposeTransport() //         order

        return success1 && success2
    }

    @objc
    public func beginUpload(
            _ resourceId: String,
            _ remoteFilePath: String,
            _ data: Data?,
            _ pipelineDepth: Int,
            _ byteAlignment: Int,
            _ initialMtuSize: Int //if zero or negative then it will be set to DefaultMtuForFileUploads
    ) -> EIOSFileUploadingInitializationVerdict {

        if !isCold() { //keep first   if another upload is already in progress we bail out
            onError("[IOSFU.BU.010] Another upload is already in progress")

            return .failedOtherUploadAlreadyInProgress
        }

        _resourceId = resourceId //it is ok if it is dud
        _remoteFilePathSanitized = remoteFilePath.trimmingCharacters(in: .whitespacesAndNewlines)
        if _remoteFilePathSanitized.isEmpty {
            onError("[IOSFU.BU.020] Target-file provided is dud")

            return .failedInvalidSettings
        }

        if _remoteFilePathSanitized.hasSuffix("/") {
            onError("[IOSFU.BU.030] Target-file points to a directory instead of a file")

            return .failedInvalidSettings
        }

        if !_remoteFilePathSanitized.hasPrefix("/") {
            onError("[IOSFU.BU.040] Target-path is not absolute")

            return .failedInvalidSettings
        }

        if data == nil { // data being nil is not ok    btw data.length==0 is perfectly ok because we might want to create empty files
            onError("[IOSFU.BU.050] The data provided are nil")

            return .failedInvalidData
        }

        if _cbPeripheral == nil {
            onError("[IOSFU.BU.060] No bluetooth-device specified - call trySetBluetoothDevice() first")

            return .failedInvalidSettings;
        }

        if (pipelineDepth >= 2 && byteAlignment <= 1) {
            onError("[IOSFU.BU.070] When pipeline-depth is set to 2 or above you must specify a byte-alignment >=2 (given byte-alignment is '\(byteAlignment)')")

            return .failedInvalidSettings
        }
        
        let byteAlignmentEnum = translateByteAlignmentMode(byteAlignment);
        if (byteAlignmentEnum == nil) {
            onError("[IOSFU.BU.080] Invalid byte-alignment value '\(byteAlignment)': It must be a power of 2 up to 16")

            return .failedInvalidSettings
        }

        if (initialMtuSize > 5_000) { //negative or zero value are ok however
            onError("[IOSFU.BU.085] Invalid mtu value '\(initialMtuSize)': Must be zero or positive and less than or equal to 5_000")

            return .failedInvalidSettings
        }

        resetState() //order
        tryDisposeFilesystemManager() //00 vital hack
        ensureTransportIsInitializedExactlyOnce(initialMtuSize) //order
        ensureFilesystemManagerIsInitializedExactlyOnce() //order

        var configuration = FirmwareUpgradeConfiguration(byteAlignment: byteAlignmentEnum!)
        if (pipelineDepth >= 0) {
            configuration.pipelineDepth = pipelineDepth
        }

        setState(.idle)

        let verdict: EIOSFileUploadingInitializationVerdict = ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                let success = _fileSystemManager?.upload( //order
                        name: _remoteFilePathSanitized,
                        data: data!,
                        using: configuration,
                        delegate: self
                ) ?? false
                if !success {
                    onError("[IOSFU.BU.090] Failed to commence file-uploading (check logs for details)")

                    return .failedErrorUponCommencing
                }

                return .success

            } catch let ex {
                onError("[IOSFU.BU.095] Failed to commence file-uploading", ex)

                return .failedErrorUponCommencing
            }
        })

        return verdict

        //00  normally we shouldnt need this   but there seems to be a bug in the lib   https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager/issues/209
        //
        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
    }
    
    private func translateByteAlignmentMode(_ alignment: Int) -> ImageUploadAlignment? {
        if (alignment <= 0) {
            return .disabled;
        }

        switch alignment {
        case 2:
            return .twoByte
        case 4:
            return .fourByte
        case 8:
            return .eightByte
        case 16:
            return .sixteenByte
        default:
            return nil
        }
    }

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    @objc
    public func tryPause() -> Bool {
        if (_currentState == .paused) { //order
            return true // already paused which is ok
        }

        if (_currentState != .uploading) { //order
            return false
        }

        if (_fileSystemManager == nil) { //order
            return false
        }
        
        return ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            if (_fileSystemManager == nil) { //vital to double-check
                return false
            }
 
            do {
                _fileSystemManager?.pauseTransfer()
                
                setState(.paused)
                setBusyState(false)

                return true
            } catch let ex {
                onError("[IOSFU.PAUSE.010] Failed to pause", ex)
                return false
            }
        })
        
        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
    }

    @objc
    public func tryResume() -> Bool {
        if _currentState == .uploading { //order
            return true //already downloading which is ok
        }

        if (_currentState != .paused) { //order
            return false
        }

        if (_fileSystemManager == nil) { //order
            return false
        }
        
        return ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            if (_fileSystemManager == nil) { //vital to double-check
                return false
            }
 
            do {
                _fileSystemManager?.continueTransfer()
                
                setState(.uploading)
                setBusyState(true)

                return true
            } catch let ex {
                onError("[IOSFU.RESUME.010] Failed to resume", ex)
                return false
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
    }

    @objc
    public func tryCancel(_ reason: String = "") -> Bool {
        _cancellationReason = reason
        DispatchQueue.global(qos: .background).async { self.cancellingAdvertisement(reason) } // order
        setState(.cancelling) //                                                                 order

        if (_fileSystemManager == nil) {
            return true
        }
        
        return ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                _fileSystemManager?.cancelTransfer() //order
                return true
            } catch let ex {
                onError("[IOSFU.CANCEL.010] Failed to cancel", ex)
                return false
            }
        })

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
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
            logMessageAdvertisement("[IOSFU.TDC.010] Failed to disconnect", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.warning.name)
            return false
        }
    }

    private func resetState() {
        _uploadStartTimestamp = nil

        _lastBytesSent = 0
        _lastBytesSentTimestamp = nil

        setState(.none)
        setBusyState(false)
    }

    private func ensureFilesystemManagerIsInitializedExactlyOnce() {
        if _fileSystemManager != nil { //already initialized
            return
        }

        _fileSystemManager = FileSystemManager(transport: _transporter) //00
        _fileSystemManager.logDelegate = self //00

        //00  this doesnt throw an error   the log-delegate aspect is implemented in the extension below via IOSFileUploader: McuMgrLogDelegate
    }

    private func ensureTransportIsInitializedExactlyOnce(_ initialMtuSize: Int) {
        let properMtu = initialMtuSize < 0 // -1=laerdal-mtu-default, 0=mtu-autoconfigured-by-nordic-libs, 1-and-above=user-mtu-custom-value
                ? Constants.DefaultMtuForFileUploads
                : initialMtuSize

        _transporter = _transporter == nil
                ? McuMgrBleTransport(_cbPeripheral)
                : _transporter

        if properMtu > 0 {
            _transporter.mtu = properMtu

            logMessageAdvertisement("[IOSFU.ETIIEO.010] applied explicit initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
        } else {
            logMessageAdvertisement("[IOSFU.ETIIEO.020] using pre-set initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
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
            logMessageAdvertisement("[IOSFU.DT.010] Failed to dispose the transport", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.warning.name)
            return false
        }
    }

    private func tryDisposeFilesystemManager() -> Bool {
        //_fileSystemManager?.cancelTransfer()  dont
        _fileSystemManager = nil
        return true
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

    //@objc   dont
    private func onError(_ errorMessage: String, _ error: Error? = nil) {
        _lastFatalErrorMessage = errorMessage

        setState(.error) //       order
        setBusyState(false) //    order

        let resourceIdSnapshot = _resourceId
        let remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.fatalErrorOccurredAdvertisement(// order
                    resourceIdSnapshot,
                    remoteFilePathSanitizedSnapshot,
                    errorMessage,
                    McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error)
            )
        }
    }

    //@objc   dont
    private func logMessageAdvertisement(_ message: String, _ category: String, _ level: String) {
        let resourceIdSnapshot = _resourceId
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.logMessageAdvertisement(message, category, level, resourceIdSnapshot)
        }
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
    private func fileUploadCompletedAdvertisement(
        _ resourceId: String?,
        _ remoteFilePathSanitized: String?
    ) {
        _listener.fileUploadCompletedAdvertisement(resourceId, remoteFilePathSanitized)
    }

    //@objc   dont
    private func busyStateChangedAdvertisement(_ busyNotIdle: Bool) {
        _listener.busyStateChangedAdvertisement(busyNotIdle)
    }

    //@objc   dont
    private func stateChangedAdvertisement(
            _ resourceId: String?,
            _ remoteFilePathSanitized: String?,
            _ oldState: EIOSFileUploaderState,
            _ newState: EIOSFileUploaderState
    ) {
        _listener.stateChangedAdvertisement(resourceId, remoteFilePathSanitized, oldState, newState)
    }

    //@objc   dont
    private func fileUploadStartedAdvertisement(
            _ resourceId: String?,
            _ remoteFilePathSanitized: String?
    ) {
        _listener.fileUploadStartedAdvertisement(
                resourceId,
                remoteFilePathSanitized
        )
    }
    
    //@objc   dont
    private func fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
            _ resourceId: String?,
            _ remoteFilePathSanitized: String?,
            _ progressPercentage: Int,
            _ currentThroughputInKbps: Float32,
            _ totalAverageThroughputInKbps: Float32
    ) {
        _listener.fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                resourceId,
                remoteFilePathSanitized,
                progressPercentage,
                currentThroughputInKbps,
                totalAverageThroughputInKbps
        )
    }

    private func setBusyState(_ newBusyState: Bool) {
        if (_currentBusyState == newBusyState) {
            return
        }

        busyStateChangedAdvertisement(newBusyState)
    }

    private func setState(_ newState: EIOSFileUploaderState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        let resourceIdSnapshot = _resourceId
        let remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self.stateChangedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, oldState, newState) //order

            switch (newState) {
            case .none: // * -> none
                self.fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, 0, 0, 0)
                break;
                
            case .uploading:
                if (oldState != .idle && oldState != .paused)
                {
                    self._listener.logMessageAdvertisement("[IFU.SS.DQGB.010] State changed to 'uploading' from an unexpected state '\(String(describing: oldState))' - this transition looks fishy so report this incident!", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.warning.name, remoteFilePathSanitizedSnapshot)
                }

                self.fileUploadStartedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot); //00
                break;
                
            case .complete:
                if (oldState != .uploading) //00
                {
                    self._listener.logMessageAdvertisement("[IFU.SS.DQGB.020] State changed to 'complete' from an unexpected state '\(String(describing: oldState))' - this transition looks fishy so report this incident!", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.warning.name, remoteFilePathSanitizedSnapshot)
                }

                self.fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, 100, 0, 0); //00   order
                self.fileUploadCompletedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot); // order
                break;
                
            default: break;
            }
        }

        //00  trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }
}

extension IOSFileUploader: FileUploadDelegate {

    public func uploadProgressDidChange(bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(.uploading)
        setBusyState(true)
        
        let resourceIdSnapshot  = _resourceId;
        let remoteFilePathSanitizedSnapshot  = _remoteFilePathSanitized;

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            let uploadProgressPercentage = (bytesSent * 100) / fileSize
            let currentThroughputInKbps = self.calculateCurrentThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)
            let totalAverageThroughputInKbps = self.calculateTotalAverageThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)

            self.fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                resourceIdSnapshot,
                remoteFilePathSanitizedSnapshot,
                uploadProgressPercentage,
                currentThroughputInKbps,
                totalAverageThroughputInKbps
            )
        }
    }

    public func uploadDidFail(with error: Error) {
        onError(error.localizedDescription, error)
        setBusyState(false)
    }

    public func uploadDidCancel() {
        setState(.cancelled)
        setBusyState(false)
        cancelledAdvertisement(_cancellationReason)
    }

    public func uploadDidFinish() {
        setState(.complete)
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
        if (_uploadStartTimestamp == nil) {
            _uploadStartTimestamp = timestamp
            return 0
        }

        let secondsSinceUploadStart = Float32(timestamp.timeIntervalSince(_uploadStartTimestamp!))
        if (secondsSinceUploadStart == 0) { //should be impossible but just in case
            return 0
        }

        return Float32(bytesSent) / (secondsSinceUploadStart * 1024)
    }

}

extension IOSFileUploader: McuMgrLogDelegate {
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
