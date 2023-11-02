import Foundation

// to future maintainers     keep this dummy class around so as to have it reference the exported classes
// to future maintainers
// to future maintainers     omitting this one causes the build environment of the azure pipelines to go completely smart-assinine and strip the
// to future maintainers     classes IOSDeviceResetter IOSFirmwareEraser and IOSFirmwareUpgrader thinking that they are not being used anywhere

public class DummyPlaceholder {
    public static func Foobar() {
        let _ = IOSDeviceResetter(nil, nil) //hack
        let _ = IOSFirmwareEraser(nil, nil) //hack
        let _ = IOSFirmwareInstaller(nil, nil) //hack

        let _ = EIOSDeviceResetterState.complete;
        let _ = EIOSFirmwareEraserState.complete;
        let _ = EIOSFirmwareInstallationState.complete;

        let _ = EIOSFirmwareInstallationMode.testAndConfirm;
        let _ = EIOSFirmwareInstallationVerdict.success;

        let _ = InvalidFirmwareInstallationModeError.runtimeError("");

        let _ :IOSListenerForDeviceResetter;
        let _ :IOSListenerForFirmwareEraser;
        let _ :IOSListenerForFirmwareInstaller;
    }
}
