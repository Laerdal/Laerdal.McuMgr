import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFirmwareInstallerX)
public class IOSFirmwareInstaller: NSObject {

    private var _manager: FirmwareUpgradeManager!
    private var _listener: IOSListenerForFirmwareInstaller!
    private var _transporter: McuMgrBleTransport!
    private var _currentState: EIOSFirmwareInstallationState
    private var _lastFatalErrorMessage: String = ""

    private var _lastBytesSend: Int = -1;
    private var _lastBytesSendTimestamp: Date? = nil;

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFirmwareInstaller!) {
        _listener = listener
        _transporter = McuMgrBleTransport(cbPeripheral)
        _currentState = .none
        _lastFatalErrorMessage = ""
    }

    private let EstimatedSwapTimeoutInMillisecondsWarningMinThreshold : Int16 = 1_000;
    
    @objc
    public func beginInstallation(
            _ imageData: Data,
            _ mode: EIOSFirmwareInstallationMode,
            _ eraseSettings: Bool,
            _ estimatedSwapTimeInMilliseconds: Int,
            _ pipelineDepth: Int,
            _ byteAlignment: Int,
            _ initialMtuSize: Int //if zero or negative then it will be set to peripheralMaxWriteValueLengthForWithoutResponse
    ) -> EIOSFirmwareInstallationVerdict {
        if !isCold() { //if another installation is already in progress we bail out
            onError(.failedInstallationAlreadyInProgress, "[IOSFI.BI.000] Another firmware installation is already in progress")

            return .failedInstallationAlreadyInProgress
        }

        _lastBytesSend = -1
        _lastBytesSendTimestamp = nil

        if (imageData.isEmpty) {
            onError(.invalidFirmware, "[IOSFI.BI.010] The firmware data-bytes given are dud")

            return .failedInvalidFirmware
        }

        if (pipelineDepth >= 2 && byteAlignment <= 1) {
            onError(.invalidSettings, "[IOSFI.BI.020] When pipeline-depth is set to 2 or above you must specify a byte-alignment >=2 (given byte-alignment is '\(byteAlignment)')")

            return .failedInvalidSettings
        }

        let byteAlignmentEnum = translateByteAlignmentMode(byteAlignment);
        if (byteAlignmentEnum == nil) {
            onError(.invalidSettings, "[IOSFI.BI.030] Invalid byte-alignment value '\(byteAlignment)': It must be a power of 2 up to 16")

            return .failedInvalidSettings
        }

        if (initialMtuSize > 5_000) { //negative or zero values are ok
            onError(.invalidSettings, "[IOSFI.BI.035] Invalid mtu value '\(initialMtuSize)': Must be zero or positive and less than or equal to 5_000")

            return .failedInvalidSettings
        }

        if (estimatedSwapTimeInMilliseconds >= 0 && estimatedSwapTimeInMilliseconds <= EstimatedSwapTimeoutInMillisecondsWarningMinThreshold) { //its better to just warn the calling environment instead of erroring out
            logMessageAdvertisement(
                    "[IOSFI.BI.040] Estimated swap-time of '\(estimatedSwapTimeInMilliseconds)' milliseconds seems suspiciously low - did you mean to say '\(estimatedSwapTimeInMilliseconds * 1_000)' milliseconds instead?",
                    "firmware-installer",
                    iOSMcuManagerLibrary.McuMgrLogLevel.warning.name
            )
        }
 
        _transporter.mtu = initialMtuSize <= 0
        ? Constants.DefaultMtuForAssetUploading
        : initialMtuSize

        _manager = FirmwareUpgradeManager(transport: _transporter, delegate: self) // the delegate aspect is implemented in the extension below
        _manager.logDelegate = self

        var firmwareUpgradeConfiguration = FirmwareUpgradeConfiguration(
                eraseAppSettings: eraseSettings,
                byteAlignment: byteAlignmentEnum!
        )

        do {
            firmwareUpgradeConfiguration.upgradeMode = try translateFirmwareInstallationMode(mode) //0

            if (pipelineDepth >= 0) {
                firmwareUpgradeConfiguration.pipelineDepth = pipelineDepth
            }

            if (estimatedSwapTimeInMilliseconds >= 0) {
                firmwareUpgradeConfiguration.estimatedSwapTime = TimeInterval(estimatedSwapTimeInMilliseconds / 1_000) //1 nRF52840 requires ~10 seconds for swapping images   adjust this parameter for your device
            }

        } catch let ex {
            onError(.invalidSettings, "[IOSFI.BI.050] Failed to configure the firmware-installer: '\(ex.localizedDescription)")

            return .failedInvalidSettings
        }

        do {
            setState(.idle)

            logMessageAdvertisement("[IOSFI.BI.055] transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)

            try _manager.start(
                    images: [
                        ImageManager.Image( //2
                                image: 0,
                                slot: 1,
                                hash: try McuMgrImage(data: imageData).hash,
                                data: imageData
                        )
                    ],
                    using: firmwareUpgradeConfiguration
            )

        } catch let ex {
            onError(.deploymentFailed, "[IOSFI.BI.060] Failed to launch the installation process: '\(ex.localizedDescription)")

            return .failedDeploymentError
        }

        return .success

        //0 set the installation mode
        //
        //1 rF52840 due to how the flash memory works requires ~20 sec to erase images
        //
        //2 the hashing algorithm is very specific to nordic   there is no practical way to go about getting it other than using the McuMgrImage utility class
    }

    private func isCold() -> Bool {
        return _currentState == .none
                || _currentState == .error
                || _currentState == .complete
                || _currentState == .cancelled
    }

    private func calculateHashBytesOfData(_ data: Data) -> Data {
        var hasher = Hasher()
        hasher.combine(data)

        let hashNumeric = hasher.finalize()

        let hashData = withUnsafeBytes(of: hashNumeric.littleEndian) { Data($0) } //00

        return hashData

        //00   notice that we have to be explicit in terms of endianess to avoid nasty surprises when transmitting bytes over the air
        //     https://stackoverflow.com/a/28681106/863651
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
        _manager?.pause()

        setState(.paused);
    }

    @objc
    public func resume() {
        _manager?.resume()

        setState(.uploading);
    }

    @objc
    public func cancel() {
        setState(.cancelling) //order

        _manager?.cancel() //order
    }

    @objc
    public func disconnect() {
        _transporter?.close()
    }

    private func onError(_ fatalErrorType: EIOSFirmwareInstallerFatalErrorType, _ errorMessage: String, _ error: Error? = nil) {
        let currentStateSnapshot = _currentState //00  order
        setState(.error) //                            order
        fatalErrorOccurredAdvertisement( //            order
                currentStateSnapshot,
                fatalErrorType,
                errorMessage,
                McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error)
        )

        //00   we want to let the calling environment know in which exact state the fatal error happened in
    }

    //@objc   dont

    private func fatalErrorOccurredAdvertisement(_ currentState: EIOSFirmwareInstallationState, _ fatalErrorType: EIOSFirmwareInstallerFatalErrorType, _ errorMessage: String, _ globalErrorCode: Int) {
        _lastFatalErrorMessage = errorMessage
        _listener.fatalErrorOccurredAdvertisement(currentState, fatalErrorType, errorMessage, globalErrorCode)
    }

    //@objc   dont

    private func logMessageAdvertisement(_ message: String, _ category: String, _ level: String) {
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.logMessageAdvertisement(message, category, level)
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
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput)
        }
    }

    private func setState(_ newState: EIOSFirmwareInstallationState) {
        if (_currentState == newState) {
            return;
        }

        let oldState = _currentState; //order

        _currentState = newState; //order

        if (oldState == .uploading && newState == .testing) //00  order
        {
            firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0);
        }

        stateChangedAdvertisement(oldState, newState); //order

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesnt fill up to 100%
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
        setState(.validating);
    }

    public func upgradeStateDidChange(from oldState: FirmwareUpgradeState, to newState: FirmwareUpgradeState) {
        switch newState {
        case .validate:
            setState(.validating);
        case .upload:
            setState(.uploading);
        case .test:
            setState(.testing);
        case .reset:
            setState(.resetting);
        case .confirm:
            setState(.confirming);
        case .success:
            setState(.complete);

        default:
            break // setState(.idle);  //dont
        }
    }

    public func upgradeDidComplete() {
        setState(.complete)
        busyStateChangedAdvertisement(false)
    }

    public func upgradeDidFail(inState state: FirmwareUpgradeState, with error: Error) {
        var fatalErrorType = EIOSFirmwareInstallerFatalErrorType.generic
        if (state == .upload) { //todo  improve this heuristic once we figure out the exact type of exception we get in case of an upload error
            fatalErrorType = .firmwareUploadingErroredOut

        } else if (state == .confirm && error.localizedDescription.isEmpty) { //todo  improve this heuristic once we figure out the exact type of exception we get in case of a swap-timeout
            fatalErrorType = .firmwareImageSwapTimeout
        }

        onError(fatalErrorType, error.localizedDescription, error)
        busyStateChangedAdvertisement(false)
    }

    public func upgradeDidCancel(state: FirmwareUpgradeState) {
        setState(.cancelled)
        busyStateChangedAdvertisement(false)
        cancelledAdvertisement()
    }

    public func uploadProgressDidChange(bytesSent: Int, imageSize: Int, timestamp: Date) {
        let throughputKilobytesPerSecond = calculateThroughput(bytesSent: bytesSent, timestamp: timestamp)
        let uploadProgressPercentage = (bytesSent * 100) / imageSize

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
