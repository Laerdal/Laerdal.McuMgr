package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareInstallationState
{
    NONE(0),
    IDLE(1),
    VALIDATING(2),
    UPLOADING(3),
    PAUSED(4),
    TESTING(5),
    RESETTING(6),
    CONFIRMING(7),
    COMPLETE(8),
    CANCELLING(9),
    CANCELLED(10),
    ERROR(11);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareInstallationState(int value)
    {
        _value = value;
    }
}
