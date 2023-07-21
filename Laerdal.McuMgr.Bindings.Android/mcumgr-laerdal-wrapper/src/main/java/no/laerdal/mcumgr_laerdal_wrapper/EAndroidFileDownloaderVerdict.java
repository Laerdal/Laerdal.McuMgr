package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileDownloaderVerdict //this must mirror the java enum values of E[Android|iOS]FileUploaderVerdict
{
    SUCCESS(0),
    FAILED__INVALID_SETTINGS(1),
    FAILED__DOWNLOAD_ALREADY_IN_PROGRESS(2);

    private final int _value;

    EAndroidFileDownloaderVerdict(int value)
    {
        _value = value;
    }
}
