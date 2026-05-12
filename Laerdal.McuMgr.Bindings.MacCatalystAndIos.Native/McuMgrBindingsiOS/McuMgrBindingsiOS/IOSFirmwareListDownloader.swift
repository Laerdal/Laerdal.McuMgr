import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFirmwareListDownloader)
public class IOSFirmwareListDownloader: NSObject {

    private var _minimumNativeLogLevel: McuMgrLogLevel = .error

    private var _listener: IOSListenerForFirmwareListDownloader!
    private var _cbPeripheral: CBPeripheral!
    private var _manager: ImageManager!
    private var _transporter: McuMgrBleTransport!
    private var _currentBusyState: Bool = false
    private var _lastFatalErrorMessage: String = ""

    @objc
    public init(_ listener: IOSListenerForFirmwareListDownloader!) {
        _listener = listener
    }

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFirmwareListDownloader!) {
        _listener = listener
        _cbPeripheral = cbPeripheral
    }

    @objc
    public func trySetBluetoothDevice(_ cbPeripheral: CBPeripheral!) -> Bool {
        if !tryInvalidateCachedInfrastructure() {
            logInBg("[IOSDI.TSBD.020] Failed to invalidate the cached-transport instance", .error)
            return false
        }

        _cbPeripheral = cbPeripheral //order

        logInBg("[IOSDI.TSBD.030] Successfully set the bluetooth-device to the given value", .verbose)

        return true
    }

    @objc
    public func tryInvalidateCachedInfrastructure() -> Bool {
        return tryDisposeTransport() //order
    }

    @objc
    public func nativeDispose() {
        logInBg("[IOSDI.ND.010] Disposing the native device-information-downloader", .verbose)

        _ = tryInvalidateCachedInfrastructure() //doesnt throw
    }

    @objc
    public func tryDisconnect() -> Bool {
        logInBg("[IOSDI.TDISC.010] Will try to disconnect now ...", .verbose)

        guard _transporter != nil else {
            logInBg("[IOSDI.TDISC.020] Transport is null so nothing to disconnect from", .verbose)
            return true
        }

        do {
            _transporter?.close()
            return true
        } catch let ex {
            logInBg("[IOSDI.TDISC.030] Failed to disconnect: '\(ex.localizedDescription)'", .error)
            return false
        }
    }

    @objc
    public func trySetMinimumNativeLogLevel(_ minimumNativeLogLevelNumeric: Int) -> Bool {
        _minimumNativeLogLevel = McuMgrLogLevelHelpers.translateLogLevel(minimumNativeLogLevelNumeric)
        return true
    }

    @objc
    public func beginDownload(_ initialMtuSize: Int, _ minimumNativeLogLevelNumeric: Int) -> String {
        if _cbPeripheral == nil {
            onError("[IOSDI.BD.040] No bluetooth-device specified - call trySetBluetoothDevice() first")
            return "FAILED__INVALID_SETTINGS"
        }

        _minimumNativeLogLevel = McuMgrLogLevelHelpers.translateLogLevel(minimumNativeLogLevelNumeric)

        do {
            ensureTransportIsInitializedExactlyOnce(initialMtuSize) //order
            _manager = ImageManager(transport: _transporter)
            _manager.logDelegate = self

            setBusyState(true) //order

            let semaphore = DispatchSemaphore(value: 0)
            var result: String = "FAILED__INVALID_DATA"

            _manager.list { response, error in
                defer { semaphore.signal() }

                if let error = error {
                    self.onError("[IOSDI.BD.050] Failed to read device information: '\(error.localizedDescription)'", error)
                    result = "FAILED__INVALID_DATA"
                    return
                }

                guard let response = response else {
                    self.onError("[IOSDI.BD.055] Received nil response from device")
                    result = "FAILED__INVALID_DATA"
                    return
                }

                result = IOSFirmwareListDownloader.parseInformation(response)
            }

            semaphore.wait()
            setBusyState(false)
            return result

        } catch let ex {
            onError("[IOSDI.BD.060] Failed to initialize the download operation", ex)
            return "FAILED__INVALID_DATA"
        }
    }

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    // MARK: - Private helpers

    private static func parseInformation(_ response: McuMgrImageStateResponse) -> String {
        do {
            var firmwareArray: [[String: Any]] = []
            for image in response.images ?? [] {
                let firmwareObject: [String: Any] = [
                    "version": image.version as Any,
                    "slot": image.slot,
                    "active": image.active,
                    "bootable": image.bootable,
                    "compressed": image.compressed,
                    "confirmed": image.confirmed,
                    "image_number": image.image,
                    "permanent": image.permanent,
                    "pending": image.pending,
                    "hash": image.hash as Any,
                ]
                firmwareArray.append(firmwareObject)
            }
            let jsonData = try JSONSerialization.data(withJSONObject: firmwareArray, options: [.prettyPrinted])
            return String(data: jsonData, encoding: .utf8) ?? "Failed to encode JSON"
        } catch {
            return "Failed to package McuMgrImageStateResponse as json\n\(error.localizedDescription)"
        }
    }

    private func ensureTransportIsInitializedExactlyOnce(_ initialMtuSize: Int) {
        if _transporter == nil {
            logInBg("[IOSDI.ETIIEO.000] Transport is null - instantiating it now", .warning)
            _transporter = McuMgrBleTransport(_cbPeripheral)
        }

        if initialMtuSize > 0 {
            _transporter.mtu = initialMtuSize
            logInBg("[IOSDI.ETIIEO.010] Initial-MTU-size set explicitly to '\(initialMtuSize)'", .info)
        } else {
            logInBg("[IOSDI.ETIIEO.020] Initial-MTU-size left to its nordic-default-value", .info)
        }
    }

    private func tryDisposeTransport() -> Bool {
        guard _transporter != nil else {
            return true // already disposed
        }

        do {
            _transporter?.close()
        } catch let ex {
            logInBg("[IOSDI.TDT.010] Failed to release the transport:\n\n\(ex)", .error)
        }

        _manager = nil
        _transporter = nil
        return true
    }

    private func setBusyState(_ newBusyState: Bool) {
        if _currentBusyState == newBusyState {
            return
        }
        _currentBusyState = newBusyState
        _listener?.busyStateChangedAdvertisement(newBusyState)
    }

    private func onError(_ errorMessage: String, _ error: Error? = nil) {
        let formattedMessage = McuMgrExceptionHelpers.formatErrorMessageWithExceptionTypeAndMessage(errorMessage, error, _transporter?.state)
        let globalErrorCode = McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error)

        _lastFatalErrorMessage = formattedMessage
        _listener?.fatalErrorOccurredAdvertisement(formattedMessage, globalErrorCode)
    }

    private static let DefaultLogCategory = "FirmwareListDownloader"

    private func logInBg(_ message: String, _ level: McuMgrLogLevel) {
        if level < _minimumNativeLogLevel {
            return
        }

        Task.fireAndForgetInTheBg { [weak self] in
            guard let self else { return }
            self._listener?.logMessageAdvertisement(message, IOSFirmwareListDownloader.DefaultLogCategory, level.name)
        }
    }
}

extension IOSFirmwareListDownloader: McuMgrLogDelegate {
    public func log(
            _ msg: String,
            ofCategory category: iOSMcuManagerLibrary.McuMgrLogCategory,
            atLevel level: iOSMcuManagerLibrary.McuMgrLogLevel
    ) {
        _listener?.logMessageAdvertisement(msg, category.rawValue, level.name)
    }
}

