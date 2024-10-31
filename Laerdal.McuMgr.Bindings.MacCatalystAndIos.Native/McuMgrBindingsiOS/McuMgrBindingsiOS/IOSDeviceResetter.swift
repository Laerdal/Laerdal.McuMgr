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
    public func beginReset(_ keepThisDummyParameter: Bool = false) -> EIOSDeviceResetInitializationVerdict {
        if (!isCold()) { //keep first
            onError("[IOSDR.BR.000] Another erasure operation is already in progress")

            return .failedOtherResetAlreadyInProgress
        }

        do {
            setState(.idle)
            _manager = DefaultManager(transport: _transporter)
            _manager.logDelegate = self

            setState(.resetting)
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
        } catch let ex {
            onError("[IOSDR.BR.030] Failed to launch the installation process: '\(ex.localizedDescription)", ex)

            return .failedErrorUponCommencing
        }

        return .success
    }

    private func isCold() -> Bool {
        return _currentState == .none
                || _currentState == .failed
                || _currentState == .complete
    }

    private func onError(_ errorMessage: String, _ error: Error? = nil) {
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
