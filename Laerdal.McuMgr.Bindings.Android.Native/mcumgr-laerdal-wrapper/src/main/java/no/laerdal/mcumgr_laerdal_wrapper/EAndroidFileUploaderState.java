package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileUploaderState
{
    NONE(0),
    IDLE(1), // this means that the uploader has been activated and will start uploading as soon as possible
    UPLOADING(2),
    PAUSED(3),
    COMPLETE(4),
    CANCELLED(5),
    ERROR(6),
    CANCELLING(7); //when a cancellation is requested

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFileUploaderState(final int value)
    {
        _value = value;
    }
}
