import Foundation
import iOSMcuManagerLibrary

internal class ThreadExecutionHelpers {
    internal static func EnsureExecutionOnMainUiThreadSync<E>(work: () -> E) -> E {
        if Thread.isMainThread {
            return work() // already on the main thread
        }

        var result: E!
        DispatchQueue.main.sync {
            result = work()
        }
        return result
    }
}
