import iOSMcuManagerLibrary
import CoreBluetooth

// @objc(IOSDeviceResetter)
public class IOSDeviceResetter: NSObject {

    private var _manager: DefaultManager!
    private var _listener: IOSListenerForDeviceResetter! //meant to be passed on from the c# world to this class   this is the cleanest way to implement listeners in swift bindings
    private var _transporter: McuMgrBleTransport!

    @objc
    public init(_ cbPeripheral: CBPeripheral!, _ listener: IOSListenerForDeviceResetter!) {
        _transporter = McuMgrBleTransport(cbPeripheral)

        _listener = listener
        _lastFatalErrorMessage = ""
    }

    @objc
    public func beginReset() {
        setState(.resetting)

        _manager = DefaultManager(transport: _transporter)
        _manager.logDelegate = self

        _manager.reset {
            response, error in

            if (error != nil) {
                self.onError("[IOSDR.BR.010] Reset failed: '\(error?.localizedDescription ?? "<unexpected error occurred>")'", error)
                return
            }

            if (response?.getError() != nil) { // check for an error return code
                self.onError("[IOSDR.BR.020] Reset failed: '\(response?.getError()?.errorDescription ?? "<N/A>")'", response?.getError())
                return
            }

            self.setState(.complete)
        }
    }

    private func onError(_ errorMessage: String, _ error: Error?) {
        setState(.failed)

        fatalErrorOccurredAdvertisement(errorMessage, McuMgrExceptionHelpers.deduceGlobalErrorCodeFromException(error))
    }

    @objc
    public func disconnect() {
        _transporter?.close()
    }

    private var _currentState = EIOSDeviceResetterState.none

    private func setState(_ newState: EIOSDeviceResetterState) {
        if (_currentState == newState) {
            return
        }

        let oldState = _currentState //order

        _currentState = newState //order

        stateChangedAdvertisement(oldState, newState) //order
    }

    private var _lastFatalErrorMessage: String

    @objc
    public func getLastFatalErrorMessage() -> String {
        _lastFatalErrorMessage
    }

    @objc
    public func getState() -> EIOSDeviceResetterState {
        _currentState
    }

    //@objc   dont
    private func fatalErrorOccurredAdvertisement(_ errorMessage: String, _ globalErrorCode: Int) {
        _lastFatalErrorMessage = errorMessage

        _listener.fatalErrorOccurredAdvertisement(errorMessage, globalErrorCode)
    }

    //@objc   dont
    private func stateChangedAdvertisement(_ oldState: EIOSDeviceResetterState, _ newState: EIOSDeviceResetterState) {
        _listener.stateChangedAdvertisement(oldState, newState)
    }
}

extension IOSDeviceResetter: McuMgrLogDelegate {
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
