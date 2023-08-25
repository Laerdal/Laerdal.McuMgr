package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareEraserState {
    NONE(0),
    IDLE(1),
    ERASING(2),
    COMPLETE(3),
    FAILED(4);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareEraserState(int value)
    {
        _value = value;
    }
}
