import Foundation
import iOSMcuManagerLibrary

internal class McuMgrExceptionHelpers {
    static func deduceGlobalErrorCodeFromException(_ error: Error? = nil) -> Int { //00
        guard let mcuMgrError = error as? McuMgrError else {
            return -99
        }

        var errorCode = -99
        var groupErrorCode = -99
        var groupSubsystemId = 0

        switch mcuMgrError {
        case .returnCode(let rc): //if smp v2 is not supported by the target device (rare these days)
            errorCode = Int(rc.rawValue)
        case .groupCode(let groupCode): //if smp v2 is supported by the target device
            groupErrorCode = Int(groupCode.rc.rawValue)
            groupSubsystemId = Int(groupCode.group)
        }

        return groupSubsystemId == 0
                ? errorCode
                : ((groupSubsystemId + 1) * 1_000 + groupErrorCode);

        //00   https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager/issues/198
    }
}
