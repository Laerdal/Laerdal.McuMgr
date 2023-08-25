@objc
public enum EIOSFirmwareInstallerFatalErrorType : Int //this must mirror the enum values of E[Android|iOS]FirmwareInstallerFatalErrorType
{
        case generic = 0
        case invalidSettings = 1
        case invalidFirmware = 2
        case deploymentFailed = 3
        case firmwareImageSwapTimeout = 4
}
