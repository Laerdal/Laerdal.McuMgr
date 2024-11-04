package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileDownloaderVerdict //this must mirror the enum values of E[Android|iOS]FileUploaderVerdict
{
    SUCCESS(0),
    FAILED__INVALID_SETTINGS(1),
    FAILED__ERROR_UPON_COMMENCING(2),
    FAILED__DOWNLOAD_ALREADY_IN_PROGRESS(3);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFileDownloaderVerdict(int value)
    {
        _value = value;
    }
}
