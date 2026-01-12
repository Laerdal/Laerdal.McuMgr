import Foundation
import iOSMcuManagerLibrary

internal class McuMgrLogLevelHelpers {
    internal static func translateLogLevel(_ minimumNativeLogLevelNumeric: Int) -> McuMgrLogLevel {
        switch minimumNativeLogLevelNumeric {
        case 0:
            return .debug
        case 1:
            return .verbose
        case 2:
            return .info
        case 3:
            return .warning
        case 4:
            return .error
        default:
            return .error
        }
    }
}


