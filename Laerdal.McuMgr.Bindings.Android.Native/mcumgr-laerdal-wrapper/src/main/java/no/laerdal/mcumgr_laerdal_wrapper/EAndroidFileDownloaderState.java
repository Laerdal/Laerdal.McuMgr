package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileDownloaderState
{
    NONE(0),
    IDLE(1),
    DOWNLOADING(2),
    PAUSED(3),
    RESUMING(4),
    COMPLETE(5),
    CANCELLED(6),
    ERROR(7),
    CANCELLING(8);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFileDownloaderState(final int value)
    {
        _value = value;
    }
}
