import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFirmwareInstallerX)
public class IOSFirmwareInstaller: NSObject {

    private var _manager: FirmwareUpgradeManager!
    private var _listener: IOSListenerForFirmwareInstaller!
    private var _transporter: McuMgrBleTransport!
    private var _cbPeripheral: CBPeripheral!
    private var _currentState: EIOSFirmwareInstallationState
    private var _currentBusyState: Bool = false
    private var _lastFatalErrorMessage: String = ""

    private var _lastBytesSent: Int = -1
    private var _uploadStartTimestamp: Date? = nil
    private var _lastBytesSentTimestamp: Date? = nil

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFirmwareInstaller!) {
        _listener = listener
        _cbPeripheral = cbPeripheral

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
            _ initialMtuSize: Int //if zero or negative then it will be set to DefaultMtuForFileUploads
    ) -> EIOSFirmwareInstallationVerdict {
        if !isCold() { //if another installation is already in progress we bail out
            onError(.installationAlreadyInProgress, "[IOSFI.BI.000] Another firmware installation is already in progress")

            return .failedInstallationAlreadyInProgress
        }

        _lastBytesSent = -1
        _uploadStartTimestamp = nil
        _lastBytesSentTimestamp = nil

        if (imageData.isEmpty) {
            onError(.givenFirmwareIsUnhealthy, "[IOSFI.BI.010] The firmware data-bytes given are dud")

            return .failedGivenFirmwareUnhealthy
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
            emitLogEntry(
                    "[IOSFI.BI.040] Estimated swap-time of '\(estimatedSwapTimeInMilliseconds)' milliseconds seems suspiciously low - did you mean to say '\(estimatedSwapTimeInMilliseconds * 1_000)' milliseconds instead?",
                    "firmware-installer",
                    iOSMcuManagerLibrary.McuMgrLogLevel.warning.name
            )
        }

        setState(.none) //                                         order
        setBusyState(false) //                                     order
        ensureTransportIsInitializedExactlyOnce(initialMtuSize) // order
        ensureFirmwareUpgradeManagerIsInitializedExactlyOnce() //  order

        let firmwareUpgradeConfiguration = spawnFirmwareUpgradeConfiguration( //order
            mode,
            eraseSettings,
            pipelineDepth,
            byteAlignmentEnum!,
            estimatedSwapTimeInMilliseconds
        )
        if firmwareUpgradeConfiguration == nil {
            return .failedInvalidSettings //no need to log here  the spawn method takes care of logging
        }

        setState(.idle)

        let verdict: EIOSFirmwareInstallationVerdict = ThreadExecutionHelpers.EnsureExecutionOnMainUiThreadSync(work: { //10
            do {
                try _manager.start(
                        images: [
                            ImageManager.Image( //20
                                    image: 0,
                                    slot: 1,
                                    hash: try McuMgrImage(data: imageData).hash,
                                    data: imageData
                            )
                        ],
                        using: firmwareUpgradeConfiguration!
                )

                return .success

            } catch let ex {
                onError(.installationInitializationFailed, "[IOSFI.BI.060] Failed to launch the installation process: '\(ex.localizedDescription)")

                return .failedInstallationInitializationErroredOut
            }
        })

        return verdict

        //10  starting from nordic libs version 1.10.1-alpha nordic devs enforced main-ui-thread affinity for all file-io operations upload/download/pause/cancel etc
        //    kinda sad really considering that we fought against such an approach but to no avail
        //
        //20  the hashing algorithm is very specific to nordic   there is no practical way to go about getting it other than using the McuMgrImage utility class
    }

    private func ensureFirmwareUpgradeManagerIsInitializedExactlyOnce() {
        if _manager != nil { //already initialized?
            return
        }

        _manager = FirmwareUpgradeManager(transport: _transporter, delegate: self) //00
        _manager.logDelegate = self

        //00  this doesnt throw an error   the log-delegate aspect is implemented in the extension below
    }

    private func ensureTransportIsInitializedExactlyOnce(_ initialMtuSize: Int) {
        let properMtu = initialMtuSize < 0 // -1=laerdal-mtu-default, 0=mtu-autoconfigured-by-nordic-libs, 1-and-above=user-mtu-custom-value
                ? Constants.DefaultMtuForFirmwareInstallations //00
                : initialMtuSize

        _transporter = _transporter == nil
                ? McuMgrBleTransport(_cbPeripheral)
                : _transporter

        if properMtu > 0 {
            _transporter.mtu = properMtu

            emitLogEntry("[IOSFI.ETIIEO.010] applied explicit initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
        } else {
            emitLogEntry("[IOSFI.ETIIEO.020] using pre-set initial-mtu-size transporter.mtu='\(String(describing: _transporter.mtu))'", McuMgrLogCategory.transport.rawValue, McuMgrLogLevel.info.name)
        }

        //00  set to DefaultMtuForFirmwareInstallations as an explicit sturdy default-mtu for the special case of ios firmware installations
        //    note that we had to resort to this because nordic 1.9.2 defaults to mtu=20(!) for some weird reason
        //    which seems to be a bug ofcourse because it causes the fw installation to fail before it even begins
    }

    private func spawnFirmwareUpgradeConfiguration(
        _ mode: EIOSFirmwareInstallationMode,
        _ eraseSettings: Bool,
        _ pipelineDepth: Int,
        _ byteAlignmentEnum: ImageUploadAlignment,
        _ estimatedSwapTimeInMilliseconds: Int
    ) -> FirmwareUpgradeConfiguration? {
        var configuration = FirmwareUpgradeConfiguration(eraseAppSettings: eraseSettings, byteAlignment: byteAlignmentEnum)

        do {
            configuration.upgradeMode = try translateFirmwareInstallationMode(mode) //0

            if (pipelineDepth > 0) { // do NOT include zero here   if the depth is set to zero then the operation will hang forever!
                configuration.pipelineDepth = pipelineDepth
            }

            if (estimatedSwapTimeInMilliseconds >= 0) {
                configuration.estimatedSwapTime = TimeInterval(estimatedSwapTimeInMilliseconds / 1_000) //1
            }

        } catch let ex {
            onError(.invalidSettings, "[IOSFI.SFUC.050] Failed to configure the firmware-installer: '\(ex.localizedDescription)")

            return nil
        }

        return configuration
        
        //1  nRF52840 requires ~10 seconds for swapping images   adjust this parameter for your device
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
        setBusyState(false) //                         order

        _lastFatalErrorMessage = errorMessage //order

        DispatchQueue.global(qos: .background).async { //order  fire and forget to boost performance
            self.fatalErrorOccurredAdvertisement(
                    currentStateSnapshot,
                    fatalErrorType,
                    errorMessage,
                    McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error)
            )
        }

        //00   we want to let the calling environment know in which exact state the fatal error happened in
    }

    //@objc   dont

    private func fatalErrorOccurredAdvertisement(_ currentState: EIOSFirmwareInstallationState, _ fatalErrorType: EIOSFirmwareInstallerFatalErrorType, _ errorMessage: String, _ globalErrorCode: Int) {
        _listener.fatalErrorOccurredAdvertisement(currentState, fatalErrorType, errorMessage, globalErrorCode)
    }

    //@objc   dont

    private func emitLogEntry(_ message: String, _ category: String, _ level: String) {
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.logMessageAdvertisement(message, category, level)
        }
    }

    //@objc   dont

    private func cancelledAdvertisement() {
        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self._listener.cancelledAdvertisement()
        }
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
            _ averageThroughput: Float32,
            _ totalAverageThroughputInKbps: Float32
    ) {
        _listener.firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                progressPercentage,
                averageThroughput,
                totalAverageThroughputInKbps
        )
    }

    private func setBusyState(_ newBusyState: Bool) {
        if (_currentBusyState == newBusyState) {
            return
        }

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            self.busyStateChangedAdvertisement(newBusyState)
        }
    }

    private func setState(_ newState: EIOSFirmwareInstallationState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            if (oldState == .uploading && newState == .testing) //00  order
            {
                self.firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0, 0)
            }

            self.stateChangedAdvertisement(oldState, newState) //order
        }

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
            throw givenFirmwareIsUnhealthyInstallationModeError.runtimeError("Mode \(mode) is invalid")
        }
    }
}

extension IOSFirmwareInstaller: FirmwareUpgradeDelegate { //todo   calculate throughput too!

    public func upgradeDidStart(controller: FirmwareUpgradeController) {
        setState(.validating);
        setBusyState(true);
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
        setBusyState(false)
    }

    public func upgradeDidFail(inState state: FirmwareUpgradeState, with error: Error) {
        let fatalErrorType = deduceInstallationFailureType(state, error)

        onError(fatalErrorType, error.localizedDescription, error)
        setBusyState(false)
    }

    private func deduceInstallationFailureType(_ state: FirmwareUpgradeState, _ error: Error) -> EIOSFirmwareInstallerFatalErrorType {
        var fatalErrorType = EIOSFirmwareInstallerFatalErrorType.generic

        switch (state) {
        case .none: //impossible to happen   should default to .generic
            break

        case .validate:
            fatalErrorType = .firmwareExtendedDataIntegrityChecksFailed //crc checks failed before the installation even commenced
            break

        case .upload:
            fatalErrorType = .firmwareUploadingErroredOut //todo  improve this heuristic once we figure out the exact type of exception we get in case of an upload error
            break

        case .test:
            fatalErrorType = .postInstallationDeviceHealthcheckTestsFailed
            break

        case .reset:
            fatalErrorType = .postInstallationDeviceRebootingFailed
            break

        case .confirm:
            fatalErrorType = error.localizedDescription.isEmpty //todo  improve this heuristic once we figure out the exact type of exception we get in case of a swap-timeout
                    ? .firmwareFinishingImageSwapTimeout
                    : .firmwarePostInstallationConfirmationFailed
            break

        default:
            break
        }

        return fatalErrorType
    }

    public func upgradeDidCancel(state: FirmwareUpgradeState) {
        setState(.cancelled)
        setBusyState(false)
        cancelledAdvertisement()
    }

    public func uploadProgressDidChange(bytesSent: Int, imageSize: Int, timestamp: Date) {
        if (imageSize == 0) {
            return
        }

        DispatchQueue.global(qos: .background).async { //fire and forget to boost performance
            let uploadProgressPercentage = (bytesSent * 100) / imageSize
            let currentThroughputInKbps = self.calculateCurrentThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)
            let totalAverageThroughputInKbps = self.calculateTotalAverageThroughputInKbps(bytesSent: bytesSent, timestamp: timestamp)

            self.firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(uploadProgressPercentage, currentThroughputInKbps, totalAverageThroughputInKbps)
        }
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

        _lastBytesSent = bytesSent //          order
        _lastBytesSentTimestamp = timestamp // order

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

extension IOSFirmwareInstaller: McuMgrLogDelegate {
    public func log(
            _ msg: String,
            ofCategory category: iOSMcuManagerLibrary.McuMgrLogCategory,
            atLevel level: iOSMcuManagerLibrary.McuMgrLogLevel
    ) {
        emitLogEntry(
                msg,
                category.rawValue,
                level.name
        )
    }
}
