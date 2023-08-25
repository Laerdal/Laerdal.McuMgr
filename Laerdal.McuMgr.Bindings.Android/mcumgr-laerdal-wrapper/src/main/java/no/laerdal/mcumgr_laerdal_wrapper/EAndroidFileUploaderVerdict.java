package no.laerdal.mcumgr_laerdal_wrapper;

public enum EAndroidFileUploaderVerdict //this must mirror the enum values of E[Android|iOS]FileUploaderVerdict
{
    SUCCESS(0),
    FAILED__INVALID_SETTINGS(1),
    FAILED__INVALID_DATA(2),
    FAILED__OTHER_UPLOAD_ALREADY_IN_PROGRESS(3);

    @SuppressWarnings("FieldCanBeLocal")
    private final int _value;

    EAndroidFileUploaderVerdict(int value)
    {
        _value = value;
    }
}

