package no.laerdal.mcumgr_laerdal_wrapper;

import io.runtime.mcumgr.dfu.FirmwareUpgradeManager;

// must always mirror the values found in   io.runtime.mcumgr.dfu.FirmwareUpgradeManager.Mode we introduced this
// mirror-enum because we don't want symbols of the underlying library to leak outside this thin java wrapper
public enum EAndroidFirmwareInstallationMode
{

    /**
     * When this mode is set, the manager will send the test and reset commands to
     * the device after the upload is complete. The device will reboot and will run the new
     * image on its next boot. If the new image supports auto-confirm feature, it will try to
     * confirm itself and change state to permanent. If not, test image will run just once
     * and will be swapped again with the original image on the next boot.
     * <p>
     * Use this mode if you just want to test the image, when it can confirm itself.
     */
    TEST_ONLY(0), // FirmwareUpgradeManager.Mode.TEST_ONLY   don't use .ordinal() here as its unreliable

    /**
     * When this flag is set, the manager will send confirm and reset commands immediately
     * after upload.
     * <p>
     * Use this mode if when the new image does not support both auto-confirm feature and
     * SMP service and could not be confirmed otherwise.
     */
    CONFIRM_ONLY(1), // FirmwareUpgradeManager.Mode.CONFIRM_ONLY   don't use .ordinal() here as its unreliable

    /**
     * When this flag is set, the manager will first send test followed by reset commands,
     * then it will reconnect to the new application and will send confirm command.
     * <p>
     * Use this mode when the new image supports SMP service and you want to test it
     * before confirming.
     */
    TEST_AND_CONFIRM(2); // FirmwareUpgradeManager.Mode.TEST_AND_CONFIRM   don't use .ordinal() here as its unreliable

    private final int _value;

    EAndroidFirmwareInstallationMode(int value) {
        _value = value;
    }

    FirmwareUpgradeManager.Mode getValueFirmwareUpgradeManagerMode() throws RuntimeException {
        switch (_value)
        {
            case 0:
                return FirmwareUpgradeManager.Mode.TEST_ONLY;
            case 1:
                return FirmwareUpgradeManager.Mode.CONFIRM_ONLY;
            case 2:
                return FirmwareUpgradeManager.Mode.TEST_AND_CONFIRM;
            default:
                throw new RuntimeException(String.format("[BUG] Value '%d' cannot be mapped to 'FirmwareUpgradeManager.Mode'", _value));
        }
    }
}
