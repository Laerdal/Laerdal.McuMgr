package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileUploaderVerdict //this must mirror the enum values of E[Android|iOS]FileUploaderVerdict
{
    SUCCESS(0),
    FAILED__INVALID_DATA(1),
    FAILED__INVALID_SETTINGS(2),
    FAILED__ERROR_UPON_COMMENCING(3), //connection problems
    FAILED__OTHER_UPLOAD_ALREADY_IN_PROGRESS(4);

    @SuppressWarnings({"FieldCanBeLocal", "unused"})
    private final int _value;

    EAndroidFileUploaderVerdict(final int value)
    {
        _value = value;
    }
}

