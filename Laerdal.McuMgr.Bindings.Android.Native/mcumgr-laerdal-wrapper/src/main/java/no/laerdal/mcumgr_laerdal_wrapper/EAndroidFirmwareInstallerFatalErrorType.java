package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareInstallerFatalErrorType //this must mirror the enum values of E[Android|iOS]FirmwareInstallerFatalErrorType
{
    GENERIC(0),
    INVALID_SETTINGS(1),
    INVALID_FIRMWARE(2),
    DEPLOYMENT_FAILED(3),
    FIRMWARE_IMAGE_SWAP_TIMEOUT(4),
    FIRMWARE_UPLOADING_ERRORED_OUT(5),
    FAILED__INSTALLATION_ALREADY_IN_PROGRESS(6);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareInstallerFatalErrorType(int value)
    {
        _value = value;
    }
}
