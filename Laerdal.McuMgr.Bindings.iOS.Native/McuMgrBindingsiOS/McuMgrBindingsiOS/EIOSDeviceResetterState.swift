@objc
public enum EIOSDeviceResetterState: Int {
    case none = 0
    case idle = 1
    case resetting = 2
    case complete = 3
    case failed = 4
}
