import Foundation
import iOSMcuManagerLibrary

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
