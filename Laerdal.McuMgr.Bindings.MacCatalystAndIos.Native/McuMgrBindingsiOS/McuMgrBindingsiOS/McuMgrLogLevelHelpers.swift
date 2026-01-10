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

internal extension Task where Success == Void, Failure == Never { //we extend the 'Task' class      todo   move this to its own file
    static func fireAndForgetInTheBg(
        _ operation: @escaping @Sendable () -> Void
    ) {
        Task<Void, Never>.detached(priority: .background) {
            do {
                operation()
            } catch {
                // logger?.warning("Fire-and-forget task failed: \(error)")
            }
        }
    }
}