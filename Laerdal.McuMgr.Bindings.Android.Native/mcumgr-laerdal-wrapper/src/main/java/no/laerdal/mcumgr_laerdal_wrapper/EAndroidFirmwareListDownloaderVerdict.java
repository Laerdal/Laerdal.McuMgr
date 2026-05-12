package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareListDownloaderVerdict //this must mirror the enum values of E[Android|iOS]FileUploaderVerdict
{
    SUCCESS(0),
    FAILED__INVALID_DATA(1),
    FAILED__INVALID_SETTINGS(2);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareListDownloaderVerdict(final int value)
    {
        _value = value;
    }
}

