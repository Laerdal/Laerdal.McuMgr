import Foundation
import iOSMcuManagerLibrary

internal class McuMgrExceptionHelpers {
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
            groupSubsystemId = 200 // custom one for this error

            groupErrorCode = 0 // generic error
            switch fileTransferError { //@formatter:off    unfortunately nordic forgot to underpin this error with numeric values for easy analysis so we have to map it manually
            case .invalidData:                groupErrorCode = 1
            case .invalidPayload:             groupErrorCode = 2
            case .missingUploadConfiguration: groupErrorCode = 3
            } //@formatter:on

        } else if let fileSystemManagerError = error as? FileSystemManagerError { //20 logInBg("[IOSFD.DGECFE.012] Detected FileSystemManagerError", McuMgrLogLevel.error)
            groupErrorCode = Int(fileSystemManagerError.rawValue)
            groupSubsystemId = 8 // SubSystemFilesystem

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
