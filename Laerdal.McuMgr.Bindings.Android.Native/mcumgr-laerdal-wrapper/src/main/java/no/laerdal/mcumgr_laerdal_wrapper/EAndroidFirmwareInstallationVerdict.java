package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareInstallationVerdict
{
                                                SUCCESS(0),      //  don't use .ordinal() here as its unreliable
                       FAILED__GIVEN_FIRMWARE_UNHEALTHY(0b0001), //  1
                               FAILED__INVALID_SETTINGS(0b0011), //  3
        FAILED__INSTALLATION_INITIALIZATION_ERRORED_OUT(0b0101), //  5
               FAILED__INSTALLATION_ALREADY_IN_PROGRESS(0b1001); //  9

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareInstallationVerdict(final int value)
    {
        _value = value;
    }
}
