import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSFirmwareEraserX)
public class IOSFirmwareEraser: NSObject {

    private var _manager: ImageManager!
    private var _listener: IOSListenerForFirmwareEraser! //meant to be passed on from the c# world to this class   this is the cleanest way to implement listeners in swift bindings
    private var _transporter: McuMgrBleTransport!

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForFirmwareEraser!) {
        _transporter = McuMgrBleTransport(cbPeripheral)

        _listener = listener
        _lastFatalErrorMessage = ""
    }

    @objc
    public func beginErasure(_ imageIndex: Int) {
        busyStateChangedAdvertisement(true)

        setState(.erasing)

        _manager = ImageManager(transport: _transporter)
        _manager.logDelegate = self

        _manager.erase {
            response, error in

            if (error != nil) {
                self.onError("[IOSFE.BE.010] Failed to start firmware erasure: \(error?.localizedDescription ?? "An unspecified error occurred")", error)
                return
            }

            self.readImageErasure()
            self.setState(.complete)
        }
    }

    @objc
    public func disconnect() {
        _transporter?.close()
    }

    private func onError(_ errorMessage: String, _ error: Error?) {
        setState(.failed)
        fatalErrorOccurredAdvertisement(errorMessage, McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error))
        busyStateChangedAdvertisement(false)
    }

    private var _lastFatalErrorMessage: String

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    //@objc   dont
    private func stateChangedAdvertisement(
            _ oldState: EIOSFirmwareEraserState,
            _ newState: EIOSFirmwareEraserState
    ) {
        _listener.stateChangedAdvertisement(oldState, newState)
    }

    //@objc   dont
    private func fatalErrorOccurredAdvertisement(_ errorMessage: String, _ globalErrorCode: Int) {
        _lastFatalErrorMessage = errorMessage //this method is meant to be overridden by csharp binding libraries to intercept updates
        _listener.fatalErrorOccurredAdvertisement(errorMessage, globalErrorCode)
    }

    //@objc   dont
    private func busyStateChangedAdvertisement(_ busyNotIdle: Bool) {
        _listener.busyStateChangedAdvertisement(busyNotIdle)
    }

    private var _currentState = EIOSFirmwareEraserState.none

    private func setState(_ newState: EIOSFirmwareEraserState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        stateChangedAdvertisement(oldState, newState) //order
    }

    private func readImageErasure() {
        busyStateChangedAdvertisement(true)

        _manager.list {
            response, error in

            if (error != nil) {
                self.onError("[IOSFE.RIE.010] Failed to read firmware images after firmware-erasure completed: \(error?.localizedDescription ?? "An unspecified error occurred")", error)
                return
            }

            self.busyStateChangedAdvertisement(false)
        }
    }
}

extension IOSFirmwareEraser: McuMgrLogDelegate {
    public func log(
            _ msg: String,
            ofCategory category: iOSMcuManagerLibrary.McuMgrLogCategory,
            atLevel level: iOSMcuManagerLibrary.McuMgrLogLevel
    ) {
        _listener.logMessageAdvertisement(
                msg,
                category.rawValue,
                level.name
        )
    }
}