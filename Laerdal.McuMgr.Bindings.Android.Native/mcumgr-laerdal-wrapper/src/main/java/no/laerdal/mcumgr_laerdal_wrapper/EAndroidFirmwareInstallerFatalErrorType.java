package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareInstallerFatalErrorType //@formatter:off   this must mirror the enum values of E[Android|iOS]FirmwareInstallerFatalErrorType
{
                                                      GENERIC(0), //   native-state=<unknown - probably a bug on our side>
                             INSTALLATION_ALREADY_IN_PROGRESS(1), //   native-state=none

                                             INVALID_SETTINGS(2), //   native-state=none
                                GIVEN_FIRMWARE_DATA_UNHEALTHY(3), //   native-state=none  cannot even unzip the given data bytes
                           INSTALLATION_INITIALIZATION_FAILED(4), //   native-state=none  call to .start() can fail if we pass invalid parameters

                 FIRMWARE_STRICT_DATA_INTEGRITY_CHECKS_FAILED(5), //   native-state=validate    which takes place before the actual installation even starts
                               FIRMWARE_UPLOADING_ERRORED_OUT(8), //   native-state=upload
            POST_INSTALLATION_DEVICE_HEALTHCHECK_TESTS_FAILED(6), //   native-state=test
                    POST_INSTALLATION_DEVICE_REBOOTING_FAILED(7), //   native-state=reset

                        FIRMWARE_FINISHING_IMAGE_SWAP_TIMEOUT(9), //   native-state=swap
               FIRMWARE_POST_INSTALLATION_CONFIRMATION_FAILED(10); //  native-state=confirm

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareInstallerFatalErrorType(final int value)
    {
        _value = value;
    }
}
