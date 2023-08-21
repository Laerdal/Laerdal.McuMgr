import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFileUploadX)
public class IOSFileUploader: NSObject {

    private var _listener: IOSListenerForFileUploader!
    private var _transporter: McuMgrBleTransport!
    private var _currentState: EIOSFileUploaderState
    private var _fileSystemManager: FileSystemManager!
    private var _lastFatalErrorMessage: String
    private var _remoteFilePathSanitized: String!

    private var _lastBytesSend: Int = -1
    private var _lastBytesSendTimestamp: Date? = nil

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFileUploader!) {
        _listener = listener
        _transporter = McuMgrBleTransport(cbPeripheral)
        _currentState = .none
        _lastFatalErrorMessage = ""
    }

    @objc
    public func beginUpload(_ remoteFilePath: String, _ data: Data) -> EIOSFileUploadingInitializationVerdict {
        if _currentState != .none && _currentState != .cancelled && _currentState != .complete && _currentState != .error { //if another upload is already in progress we bail out
            return EIOSFileUploadingInitializationVerdict.failedOtherUploadAlreadyInProgress
        }

        _lastBytesSend = -1
        _lastBytesSendTimestamp = nil

        _remoteFilePathSanitized = remoteFilePath.trimmingCharacters(in: .whitespacesAndNewlines)
        if _remoteFilePathSanitized.isEmpty {
            setState(EIOSFileUploaderState.error)
            fatalErrorOccurredAdvertisement("Target-file provided is dud")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        if _remoteFilePathSanitized.hasSuffix("/") {
            setState(EIOSFileUploaderState.error)
            fatalErrorOccurredAdvertisement("Target-file points to a directory instead of a file")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        if !_remoteFilePathSanitized.hasPrefix("/") {
            setState(EIOSFileUploaderState.error)
            fatalErrorOccurredAdvertisement("Target-path is not absolute!")

            return EIOSFileUploadingInitializationVerdict.failedInvalidSettings
        }

        // if (data == nil) { // data being null is not ok but in swift Data can never be null anyway   btw data.length==0 is perfectly ok because we might want to create empty files
        //      setState(EAndroidFileUploaderState.ERROR);
        //      fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, "Provided data is null");
        //
        //      return EAndroidFileUploaderVerdict.FAILED__INVALID_DATA;
        // }

        _fileSystemManager = FileSystemManager(transporter: _transporter) // the delegate aspect is implemented in the extension below
        _fileSystemManager.logDelegate = self

        setState(EIOSFileUploaderState.idle)
        busyStateChangedAdvertisement(true)
        fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(0, 0)

        let success = _fileSystemManager.upload(name: _remoteFilePathSanitized, data: data, delegate: self)
        if !success {
            setState(EIOSFileUploaderState.error)
            fatalErrorOccurredAdvertisement("Failed to commence file-uploading (check logs for details)")

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
        _transporter?.close()
    }

    //@objc   dont
    private func fatalErrorOccurredAdvertisement(_ errorMessage: String) {
        _lastFatalErrorMessage = errorMessage

        _listener.fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, errorMessage)
    }

    //@objc   dont
    private func logMessageAdvertisement(_ message: String, _ category: String, _ level: String) {
        _listener.logMessageAdvertisement(_remoteFilePathSanitized, message, category, level)
    }

    //@objc   dont
    private func cancelledAdvertisement() {
        _listener.cancelledAdvertisement()
    }

    //@objc   dont
    private func uploadCompletedAdvertisement() {
        _listener.uploadCompletedAdvertisement(_remoteFilePathSanitized)
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
    private func fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(
            _ progressPercentage: Int,
            _ averageThroughput: Float32
    ) {
        _listener.fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(
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
            fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(100, 0)
        }

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }
}

extension IOSFileUploader: FileUploadDelegate {

    public func uploadProgressDidChange(bytesSent: Int, fileSize: Int, timestamp: Date) {
        setState(EIOSFileUploaderState.uploading)
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let uploadProgressPercentage = (bytesSent * 100) / fileSize
        fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(uploadProgressPercentage, throughputKilobytesPerSecond)
    }

    public func uploadDidFail(with error: Error) {
        setState(EIOSFileUploaderState.error)
        fatalErrorOccurredAdvertisement(error.localizedDescription)
        busyStateChangedAdvertisement(false)
    }

    public func uploadDidCancel() {
        setState(EIOSFileUploaderState.cancelled)
        busyStateChangedAdvertisement(false)
        fileUploadProgressPercentageAndThroughputDataChangedAdvertisement(0, 0)
        cancelledAdvertisement()
    }

    public func uploadDidFinish() {
        setState(EIOSFileUploaderState.complete)
        uploadCompletedAdvertisement()
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
