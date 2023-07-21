package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileUploaderState
{
    NONE(0),
    IDLE(1),
    UPLOADING(2),
    PAUSED(3),
    COMPLETE(4),
    CANCELLED(5),
    ERROR(6),
    CANCELLING(7); //when a cancellation is requested

    private final int _value;

    EAndroidFileUploaderState(int value)
    {
        _value = value;
    }
}
