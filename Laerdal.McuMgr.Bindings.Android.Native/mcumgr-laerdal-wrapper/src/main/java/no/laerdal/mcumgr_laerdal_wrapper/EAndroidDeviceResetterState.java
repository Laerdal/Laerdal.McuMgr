package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidDeviceResetterState
{
    NONE(0),
    IDLE(1),
    RESETTING(2),
    COMPLETE(3),
    FAILED(4);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidDeviceResetterState(final int value)
    {
        _value = value;
    }
}
