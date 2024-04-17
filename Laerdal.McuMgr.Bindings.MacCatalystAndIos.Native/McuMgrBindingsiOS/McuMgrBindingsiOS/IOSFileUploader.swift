import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileUploadX)
public class IOSFileUploader: NSObject {

    private let _listener: IOSListenerForFileUploader!
    private var _transporter: McuMgrBleTransport!
    private var _cbPeripheral: CBPeripheral!
    private var _currentState: EIOSFileUploaderState = .none
    private var _fileSystemManager: FileSystemManager!
    private var _lastFatalErrorMessage: String = ""
    private var _remoteFilePathSanitized: String!

    private var _lastBytesSend: Int = 0
    private var _lastBytesSendTimestamp: Date? = nil

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
        if !IsCold() {
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

        if !IsCold() { //if the upload is already in progress we bail out
            return false
        }

        disposeFilesystemManager() // order
        disposeTransport() //         order

        return true;
    }

    @objc
    public func beginUpload(_ remoteFilePath: String, _ data: Data) -> EIOSFileUploadingInitializationVerdict {
        if !IsCold() { //if another upload is already in progress we bail out
            setState(EIOSFileUploaderState.error)
            onError("Another upload is already in progress")

            return EIOSFileUploadingInitializationVerdict.failedOtherUploadAlreadyInProgress
        }

        if _cbPeripheral == nil {
            setState(EIOSFileUploaderState.error);
            onError("No bluetooth-device specified - call trySetBluetoothDevice() first");

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings;
        }

        _remoteFilePathSanitized = remoteFilePath.trimmingCharacters(in: .whitespacesAndNewlines)
        if _remoteFilePathSanitized.isEmpty {
            setState(EIOSFileUploaderState.error)
            onError("Target-file provided is dud")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        if _remoteFilePathSanitized.hasSuffix("/") {
            setState(EIOSFileUploaderState.error)
            onError("Target-file points to a directory instead of a file")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        if !_remoteFilePathSanitized.hasPrefix("/") {
            setState(EIOSFileUploaderState.error)
            onError("Target-path is not absolute!")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        // if data == nil { // data being nil is not ok but in swift Data can never be nil anyway   btw data.length==0 is perfectly ok because we might want to create empty files
        //      return EIOSFileUploaderVerdict.FAILED__INVALID_DATA
        // }

        ensureTransportIsInitializedExactlyOnce() //order
        ensureFilesystemManagerIsInitializedExactlyOnce() //order

        resetUploadState() //order

        let success = _fileSystemManager.upload( //order
                name: _remoteFilePathSanitized,
                data: data,
                delegate: self
        )
        if !success {
            setState(EIOSFileUploaderState.error)
            onError("Failed to commence file-uploading (check logs for details)")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        return EIOSFileUploadingInitializationVerdict.success
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
    public func cancel() {
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

        setState(EIOSFileUploaderState.idle)
        busyStateChangedAdvertisement(true)
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
    }

    private func ensureFilesystemManagerIsInitializedExactlyOnce() {
        if _fileSystemManager != nil { //already initialized
            return
        }

        _fileSystemManager = FileSystemManager(transporter: _transporter) //00
        _fileSystemManager.logDelegate = self //00

        //00  this doesnt throw an error   the log-delegate aspect is implemented in the extension below via IOSFileUploader: McuMgrLogDelegate
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

    private func IsCold() -> Bool {
        return _currentState == EIOSFileUploaderState.none
                || _currentState == EIOSFileUploaderState.error
                || _currentState == EIOSFileUploaderState.complete
                || _currentState == EIOSFileUploaderState.cancelled
    }

    //@objc   dont
    private func onError(_ errorMessage: String, _ error: Error? = nil) {
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
                : (Int(matchesArray[0].trimmingCharacters(in: .whitespaces).trimmingCharacters(in: [ "(", ")", "." ]).trimmingCharacters(in: .whitespaces)) ?? 0)

        return (errorCode, possibleError)
    }

    private func matches(for regex: String, in text: String) -> ([String], String?) { //00
        do {
            let regex = try NSRegularExpression(pattern: regex)
            let results = regex.matches(in: text, range: NSRange(text.startIndex..., in: text))

            return (
                    results.map { String(text[Range($0.range, in: text)!]) },
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
        _listener.fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                progressPercentage,
                averageThroughput
        )
    }

    private func setState(_ newState: EIOSFileUploaderState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        stateChangedAdvertisement(oldState, newState) //order

        if (oldState == EIOSFileUploaderState.uploading && newState == EIOSFileUploaderState.complete) //00
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0)
        }

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }
}

extension IOSFileUploader: FileUploadDelegate {

    public func uploadProgressDidChange(bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(EIOSFileUploaderState.uploading)
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let uploadProgressPercentage = (bytesSent * 100) / fileSize
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(uploadProgressPercentage, throughputKilobytesPerSecond)
    }

    public func uploadDidFail(with error: Error) {
        setState(EIOSFileUploaderState.error)
        onError(error.localizedDescription, error)
        busyStateChangedAdvertisement(false)
    }

    public func uploadDidCancel() {
        setState(EIOSFileUploaderState.cancelled)
        busyStateChangedAdvertisement(false)
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)
        cancelledAdvertisement()
    }

    public func uploadDidFinish() {
        setState(EIOSFileUploaderState.complete)
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
