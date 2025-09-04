package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFirmwareEraserInitializationVerdict
{
    SUCCESS(0),
    FAILED__ERROR_UPON_COMMENCING(1), //connection problems
    FAILED__OTHER_ERASURE_ALREADY_IN_PROGRESS(2);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFirmwareEraserInitializationVerdict(final int value)
    {
        _value = value;
    }
}
