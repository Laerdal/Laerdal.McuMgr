import Foundation
import iOSMcuManagerLibrary

internal class McuMgrExceptionHelpers {
    internal static func formatErrorMessageWithExceptionTypeAndMessage(_ errorMessage: String, _ error: Error? = nil, _ transporterState: PeripheralState? = nil) -> String {
        let transporterStateDescription: String = {
            if let state = transporterState {
                return String(describing: state)
            } else {
                return "nil"
            }
        }()

        guard let error else {
            return "[NativeErrorType: nil] [NativeTransporterState: \(transporterStateDescription)] \(errorMessage)"
        }

        if (errorMessage != error.localizedDescription) {
            return "[NativeErrorType: \(type(of: error))] [NativeTransporterState: \(transporterStateDescription)] \(errorMessage) : \(error.localizedDescription)"
        }

        return "[NativeErrorType: \(type(of: error))] [NativeTransporterState: \(transporterStateDescription)] \(errorMessage)"
    }

    internal static func deduceGlobalErrorCodeFromException(_ error: Error? = nil) -> Int { //00
        // if let error {
        //     logInBg("[IOSFD.DGECFE.010] Error: '\(error.localizedDescription)' - type: \(type(of: error))", McuMgrLogLevel.error)
        // } else {
        //     logInBg("[IOSFD.DGECFE.010] Error: '(N/A!?)' - type: \(type(of: error))", McuMgrLogLevel.error)
        // }

        var errorCode = -99
        var groupErrorCode = -99
        var groupSubsystemId = 0

        if let fileTransferError = error as? FileTransferError { //20  logInBg("[IOSFD.DGECFE.012] Detected FileTransferError", McuMgrLogLevel.error)
            groupSubsystemId = 200 // SubSystemFileTransporter_*    must match the base-error-code used in EGlobalErrorCode.cs

            groupErrorCode = 0 // generic error
            switch fileTransferError { //@formatter:off             unfortunately nordic forgot to underpin this error with numeric values for easy analysis so we have to map it manually
            case .invalidData:                groupErrorCode = 1 // bear in mind that the values we use here must absolutely match between ios and android
            case .invalidPayload:             groupErrorCode = 2
            case .missingUploadConfiguration: groupErrorCode = 3
            } //@formatter:on

        } else if let mcuMgrTransportError = error as? McuMgrTransportError { //20 logInBg("[IOSFD.DGECFE.012] Detected McuMgrTransportError", McuMgrLogLevel.error)

            groupSubsystemId = 300 // SubSystemMcuMgrTransport_*    must match the base-error-code used in EGlobalErrorCode.cs

            groupErrorCode = 0 // generic error
            switch mcuMgrTransportError { //@formatter:off    unfortunately nordic forgot to underpin this error with numeric values for easy analysis so we have to map it manually
            case .badHeader:                                     groupErrorCode =  1
            case .sendFailed:                                    groupErrorCode =  2
            case .sendTimeout:                                   groupErrorCode =  3 // we get a send-timeout on abrupt disconnection due to the remote device going out of range or out of battery
            case .badChunking:                                   groupErrorCode =  4
            case .badResponse:                                   groupErrorCode =  5
            case .disconnected:                                  groupErrorCode =  6 // we would expect to get this but upon abrupt disconnection but we dont get it at all   we get SubSystemMcuMgrTransport_SendTimeout instead
            case .waitAndRetry:                                  groupErrorCode =  7
            case .insufficientMtu(mtu: let mtu):                 groupErrorCode =  8
            case .connectionFailed:                              groupErrorCode =  9
            case .connectionTimeout:                             groupErrorCode = 10
            case .peripheralNotReadyForWriteWithoutResponse:     groupErrorCode = 11
            } //@formatter:on

        } else if let fileSystemManagerError = error as? FileSystemManagerError { //20 logInBg("[IOSFD.DGECFE.012] Detected FileSystemManagerError", McuMgrLogLevel.error)
            groupErrorCode = Int(fileSystemManagerError.rawValue)
            groupSubsystemId = 8 // SubSystemFilesystem_*   must match the base-error-code used in EGlobalErrorCode.cs

        } else {
            guard let mcuMgrError = error as? McuMgrError else { // logInBg("[IOSFD.DGECFE.015]", McuMgrLogLevel.error)
                return -99
            }

            switch mcuMgrError {
            case .returnCode(let rc): //if smp v2 is not supported by the target device (rare these days)
                errorCode = Int(rc.rawValue)
            case .groupCode(let groupCode): //if smp v2 is supported by the target device
                groupErrorCode = Int(groupCode.rc.rawValue)
                groupSubsystemId = Int(groupCode.group)
            }
        }

        let eventualErrorCode = groupSubsystemId == 0
                ? errorCode
                : ((groupSubsystemId + 1) * 1_000 + groupErrorCode) // logInBg("[IOSFD.DGECFE.100] eventualErrorCode=\(eventualErrorCode)", McuMgrLogLevel.error)

        return eventualErrorCode

        //00   https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager/issues/198
        //
        //20   in ios (unlike android) we do get non-mcumgr exceptions and we have to process
        //     them properly to deduce the appropriate eventual compound-error-code
    }
}
