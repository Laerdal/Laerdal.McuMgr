package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileUploaderState
{
    NONE(0),
    IDLE(1), // this means that the uploader has been activated and will start uploading as soon as possible
    UPLOADING(2),
    PAUSED(3),
    RESUMING(4),
    COMPLETE(5),
    CANCELLED(6),
    ERROR(7),
    CANCELLING(8); //when a cancellation is requested

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFileUploaderState(final int value)
    {
        _value = value;
    }
}
