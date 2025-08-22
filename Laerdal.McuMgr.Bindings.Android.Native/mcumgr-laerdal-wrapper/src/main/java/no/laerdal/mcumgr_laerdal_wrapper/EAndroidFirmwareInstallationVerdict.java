package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareInstallationVerdict //@formatter:off    must be aligned with the respective verdict-enums in ios and the csharp lib
{
                                                SUCCESS(0),       //  don't use .ordinal() here as its unreliable
                                              // FAILED(0b00001), //  1     basic failure-flag
                       FAILED__GIVEN_FIRMWARE_UNHEALTHY(0b00011), //  3
                               FAILED__INVALID_SETTINGS(0b00101), //  5
        FAILED__INSTALLATION_INITIALIZATION_ERRORED_OUT(0b01001), //  9
               FAILED__INSTALLATION_ALREADY_IN_PROGRESS(0b10001); //  17

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareInstallationVerdict(final int value)
    {
        _value = value;
    }
}
