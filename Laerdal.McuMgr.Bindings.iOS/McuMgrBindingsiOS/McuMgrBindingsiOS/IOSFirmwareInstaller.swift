import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFirmwareInstallerX)
public class IOSFirmwareInstaller: NSObject {

    private var _manager: FirmwareUpgradeManager!
    private var _listener: IOSListenerForFirmwareInstaller!
    private var _transporter: McuMgrBleTransport!
    private var _currentState: EIOSFirmwareInstallationState
    private var _lastFatalErrorMessage: String;

    private var _lastBytesSend: Int = -1;
    private var _lastBytesSendTimestamp: Date? = nil;

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFirmwareInstaller!) {
        _listener = listener
        _transporter = McuMgrBleTransport(cbPeripheral)
        _currentState = .none
        _lastFatalErrorMessage = ""
    }

    @objc
    public func beginInstallation(
            _ imageData: Data,
            _ mode: EIOSFirmwareInstallationMode,
            _ eraseSettings: Bool,
            _ estimatedSwapTimeInMilliseconds: Int,
            _ pipelineDepth: Int,
            _ byteAlignment: Int
    ) -> EIOSFirmwareInstallationVerdict {
        if _currentState != .none && _currentState != .cancelled && _currentState != .complete && _currentState != .error { //if another installation is already in progress we bail out
            return EIOSFirmwareInstallationVerdict.failedInstallationAlreadyInProgress
        }

        _lastBytesSend = -1
        _lastBytesSendTimestamp = nil

        if (pipelineDepth >= 2 && byteAlignment <= 1) {
            emitFatalError("When pipeline-depth is set to 2 or above you must specify a byte-alignment >=2 (given byte-alignment is '\(byteAlignment)')")

            return EIOSFirmwareInstallationVerdict.failedInvalidSettings
        }

        let byteAlignmentEnum = translateByteAlignmentMode(byteAlignment);
        if (byteAlignmentEnum == nil) {
            emitFatalError("Invalid byte-alignment value '\(byteAlignment)': It must be a power of 2 up to 16")

            return EIOSFirmwareInstallationVerdict.failedInvalidSettings
        }

        if (estimatedSwapTimeInMilliseconds >= 0 && estimatedSwapTimeInMilliseconds <= 1000) { //its better to just warn the calling environment instead of erroring out
            logMessageAdvertisement(
                    "Estimated swap-time of '\(estimatedSwapTimeInMilliseconds)' milliseconds seems suspiciously low - did you mean to say '\(estimatedSwapTimeInMilliseconds * 1000)' milliseconds?",
                    "firmwareinstaller",
                    iOSMcuManagerLibrary.McuMgrLogLevel.warning.name
            );
        }

        _manager = FirmwareUpgradeManager(transporter: _transporter, delegate: self) // the delegate aspect is implemented in the extension below
        _manager.logDelegate = self

        var firmwareUpgradeConfiguration = FirmwareUpgradeConfiguration(
                eraseAppSettings: eraseSettings,
                byteAlignment: byteAlignmentEnum!
        )

        do {
            _manager.mode = try translateFirmwareInstallationMode(mode) //0

            if (pipelineDepth >= 0) {
                firmwareUpgradeConfiguration.pipelineDepth = pipelineDepth
            }

            if (estimatedSwapTimeInMilliseconds >= 0) {
                firmwareUpgradeConfiguration.estimatedSwapTime = TimeInterval(estimatedSwapTimeInMilliseconds / 1000) //1 nRF52840 requires ~10 seconds for swapping images   adjust this parameter for your device
            }

        } catch let ex {
            emitFatalError(ex.localizedDescription);

            return EIOSFirmwareInstallationVerdict.failedInvalidSettings;
        }

        do {
            setState(EIOSFirmwareInstallationState.idle)
            firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0)

            try _manager.start(data: imageData, using: firmwareUpgradeConfiguration);

        } catch let ex {
            emitFatalError(ex.localizedDescription);

            return EIOSFirmwareInstallationVerdict.failedDeploymentError;
        }

        return EIOSFirmwareInstallationVerdict.success;

        //0 set the installation mode
        //
        //1 rF52840 due to how the flash memory works requires ~20 sec to erase images
        //
        //3 set the selected memory alignment  in the app this defaults to 4 to match nordic devices but can be modified in the ui
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
        _manager.pause()

        setState(.paused);
    }

    @objc
    public func resume() {
        _manager.resume()

        setState(.uploading);
    }

    @objc
    public func cancel() {
        setState(.cancelling) //order

        _manager.cancel() //order
    }

    @objc
    public func disconnect() {
        _transporter.close()
    }

    private func emitFatalError(_ errorMessage: String) {
        let currentStateSnapshot = _currentState //00

        setState(.error) //                                                      order
        fatalErrorOccurredAdvertisement(currentStateSnapshot, errorMessage) //   order

        //00   we want to let the calling environment know in which exact state the fatal error happened in
    }

    //@objc   dont

    private func fatalErrorOccurredAdvertisement(_ currentState: EIOSFirmwareInstallationState, _ errorMessage: String) {
        _lastFatalErrorMessage = errorMessage
        _listener.fatalErrorOccurredAdvertisement(currentState, errorMessage)
    }

    //@objc   dont

    private func logMessageAdvertisement(_ message: String, _ category: String, _ level: String) {
        _listener.logMessageAdvertisement(message, category, level);
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
            _ oldState: EIOSFirmwareInstallationState,
            _ newState: EIOSFirmwareInstallationState
    ) {
        _listener.stateChangedAdvertisement(oldState, newState)
    }

    //@objc   dont

    private func firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(
            _ progressPercentage: Int,
            _ averageThroughput: Float32
    ) {
        _listener.firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput)
    }

    private func setState(_ newState: EIOSFirmwareInstallationState) {
        if (_currentState == newState) {
            return;
        }

        let oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(oldState, newState); //order

        if (oldState == EIOSFirmwareInstallationState.uploading && newState == EIOSFirmwareInstallationState.testing) //00
        {
            firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0);
        }

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }

    private func translateFirmwareInstallationMode(_ mode: EIOSFirmwareInstallationMode) throws -> FirmwareUpgradeMode {
        switch mode {

        case .testOnly: //0
            return FirmwareUpgradeMode.testOnly
        case .confirmOnly: //1
            return FirmwareUpgradeMode.confirmOnly
        case .testAndConfirm: //3
            return FirmwareUpgradeMode.testAndConfirm

        default:
            throw InvalidFirmwareInstallationModeError.runtimeError("Mode \(mode) is invalid")
        }
    }
}

extension IOSFirmwareInstaller: FirmwareUpgradeDelegate { //todo   calculate throughput too!

    public func upgradeDidStart(controller: FirmwareUpgradeController) {
        busyStateChangedAdvertisement(true);
        firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
        setState(EIOSFirmwareInstallationState.validating);
    }

    public func upgradeStateDidChange(from oldState: FirmwareUpgradeState, to newState: FirmwareUpgradeState) {
        switch newState {
        case .validate:
            setState(EIOSFirmwareInstallationState.validating);
        case .upload:
            setState(EIOSFirmwareInstallationState.uploading);
        case .test:
            setState(EIOSFirmwareInstallationState.testing);
        case .confirm:
            setState(EIOSFirmwareInstallationState.confirming);
        case .reset:
            setState(EIOSFirmwareInstallationState.resetting);
        case .success:
            setState(EIOSFirmwareInstallationState.complete);
        default:
            setState(EIOSFirmwareInstallationState.idle);
        }
    }

    public func upgradeDidComplete() {
        setState(EIOSFirmwareInstallationState.complete)
        busyStateChangedAdvertisement(false)
    }

    public func upgradeDidFail(inState state: FirmwareUpgradeState, with error: Error) {
        emitFatalError(error.localizedDescription)
        busyStateChangedAdvertisement(false)
    }

    public func upgradeDidCancel(state: FirmwareUpgradeState) {
        setState(EIOSFirmwareInstallationState.cancelled)
        busyStateChangedAdvertisement(false)
        firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
        cancelledAdvertisement()
    }

    public func uploadProgressDidChange(bytesSent: Int, imageSize: Int, timestamp: Date) {
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let uploadProgressPercentage = (bytesSent * 100) / imageSize;

        firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(uploadProgressPercentage, throughputKilobytesPerSecond);
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

extension IOSFirmwareInstaller: McuMgrLogDelegate {
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
