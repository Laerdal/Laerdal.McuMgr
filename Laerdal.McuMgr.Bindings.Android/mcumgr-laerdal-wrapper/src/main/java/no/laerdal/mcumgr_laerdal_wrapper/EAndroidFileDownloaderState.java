package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileDownloaderState
{
    NONE(0),
    IDLE(1),
    DOWNLOADING(2),
    PAUSED(3),
    COMPLETE(4),
    CANCELLED(5),
    ERROR(6),
    CANCELLING(7);

    private final int _value;

    EAndroidFileDownloaderState(int value)
    {
        _value = value;
    }
}
