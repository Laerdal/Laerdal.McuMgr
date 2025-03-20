import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileUploadX)
public class IOSFileUploader: NSObject {

    private let _listener: IOSListenerForFileUploader!
    private var _transporter: McuMgrBleTransport!
    private var _cbPeripheral: CBPeripheral!
    private var _fileSystemManager: FileSystemManager!

    private var _currentState: EIOSFileUploaderState = .none
    private var _lastBytesSend: Int = 0
    private var _cancellationReason: String = ""
    private var _lastFatalErrorMessage: String = ""
    private var _lastBytesSendTimestamp: Date? = nil
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
    public func beginUpload(
            _ remoteFilePath: String,
            _ data: Data?,
            _ pipelineDepth: Int,
            _ byteAlignment: Int,
            _ initialMtuSize: Int //if zero or negative then it will be set to peripheralMaxWriteValueLengthForWithoutResponse
    ) -> EIOSFileUploadingInitializationVerdict {

        if !isCold() { //keep first   if another upload is already in progress we bail out
            onError("[IOSFU.BU.010] Another upload is already in progress")

            return .failedOtherUploadAlreadyInProgress
        }

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
            onError("[IOSFU.BU.060] No bluetooth-device specified - call trySetBluetoothDevice() first");

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

        resetUploadState() //order
        disposeFilesystemManager() //00 vital hack
        ensureTransportIsInitializedExactlyOnce(initialMtuSize) //order
        ensureFilesystemManagerIsInitializedExactlyOnce() //order

        var configuration = FirmwareUpgradeConfiguration(byteAlignment: byteAlignmentEnum!)
        if (pipelineDepth >= 0) {
            configuration.pipelineDepth = pipelineDepth
        }

        do
        {
            let success = _fileSystemManager.upload( //order
                    name: _remoteFilePathSanitized,
                    data: data!,
                    using: configuration,
                    delegate: self
            )
            if !success {
                onError("[IOSFU.BU.090] Failed to commence file-uploading (check logs for details)")

                return .failedErrorUponCommencing
            }
        }
        catch let error //even though static analysis claims that no exception can be thrown it is in fact possible for the .upload() method to crash due to mtu related errors!
        {
            onError("[IOSFU.BU.095] Failed to commence file-uploading (check logs for details)", error)

            return .failedErrorUponCommencing
        }

        return .success

        //00  normally we shouldnt need this   but there seems to be a bug in the lib   https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager/issues/209
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
    public func pause() {
        _fileSystemManager?.pauseTransfer()

        setState(.paused)
    }

    @objc
    public func resume() {
        _fileSystemManager?.continueTransfer()

        setState(.uploading)
    }

    @objc
    public func cancel(_ reason: String = "") {
        _cancellationReason = reason

        cancellingAdvertisement(reason)
        setState(.cancelling) //order

        _fileSystemManager?.cancelTransfer() //order
    }

    @objc
    public func disconnect() {
        disposeTransport()
    }

    private func resetUploadState() {
        _lastBytesSend = 0
        _lastBytesSendTimestamp = nil

        setState(.idle)
        busyStateChangedAdvertisement(true)
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
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
        let properMtu = initialMtuSize <= 0
        ? Constants.DefaultMtuForAssetUploading
        : initialMtuSize
        
        if _transporter != nil {
            _transporter.mtu = properMtu
            return
        }
        
        _transporter = McuMgrBleTransport(_cbPeripheral)
        _transporter.mtu = properMtu
        
        logMessageAdvertisement(
            "[native::ensureTransportIsInitializedExactlyOnce()] transport initialized with mtu='\(String(describing: _transporter.mtu))'",
            McuMgrLogCategory.transport.rawValue,
            McuMgrLogLevel.info.name
        )
    }

    private func disposeTransport() {
        _transporter?.close()
        _transporter = nil
    }

    private func disposeFilesystemManager() {
        //_fileSystemManager?.cancelTransfer()  dont
        _fileSystemManager = nil
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

        setState(.error) //                           order
        _listener.fatalErrorOccurredAdvertisement( // order
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
    private func cancellingAdvertisement(_ reason: String) {
        _listener.cancellingAdvertisement(reason)
    }

    //@objc   dont
    private func cancelledAdvertisement(_ reason: String) {
        _listener.cancelledAdvertisement(reason)
    }

    //@objc   dont
    private func fileUploadedAdvertisement() {
        _listener.fileUploadedAdvertisement(_remoteFilePathSanitized)
    }

    //@objc   dont
    private func busyStateChangedAdvertisement(_ busyNotIdle: Bool) {
        _listener.busyStateChangedAdvertisement(busyNotIdle)
    }

    //@objc   dont
    private func stateChangedAdvertisement(
            _ oldState: EIOSFileUploaderState,
            _ newState: EIOSFileUploaderState
    ) {
        _listener.stateChangedAdvertisement(_remoteFilePathSanitized, oldState, newState)
    }

    //@objc   dont
    private func fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
            _ progressPercentage: Int,
            _ averageThroughput: Float32
    ) {
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    progressPercentage,
                    averageThroughput
            )
        }
    }

    private func setState(_ newState: EIOSFileUploaderState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        stateChangedAdvertisement(oldState, newState) //order

        if (oldState == .uploading && newState == .complete) //00
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0)
        }

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }
}

extension IOSFileUploader: FileUploadDelegate {

    public func uploadProgressDidChange(bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(.uploading)
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let uploadProgressPercentage = (bytesSent * 100) / fileSize
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(uploadProgressPercentage, throughputKilobytesPerSecond)
    }

    public func uploadDidFail(with error: Error) {
        onError(error.localizedDescription, error)
        busyStateChangedAdvertisement(false)
    }

    public func uploadDidCancel() {
        setState(.cancelled)
        busyStateChangedAdvertisement(false)
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
        cancelledAdvertisement(_cancellationReason)
    }

    public func uploadDidFinish() {
        setState(.complete)
        fileUploadedAdvertisement()
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
