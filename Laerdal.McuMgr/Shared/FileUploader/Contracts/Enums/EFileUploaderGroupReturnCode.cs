namespace Laerdal.McuMgr.FileUploader.Contracts.Enums
{
    public enum EFileUploaderGroupReturnCode //@formatter:off
    {
        OK                        = 0,
        UNKNOWN                   = 1,
        INVALID_NAME              = 2,
        NOT_FOUND                 = 3,
        IS_DIRECTORY              = 4,
        OPEN_FAILED               = 5,
        SEEK_FAILED               = 6,
        READ_FAILED               = 7,
        TRUNCATE_FAILED           = 8,
        DELETE_FAILED             = 9,
        WRITE_FAILED              = 10,
        OFFSET_NOT_VALID          = 11,
        OFFSET_LARGER_THAN_FILE   = 12,
        CHECKSUM_HASH_NOT_FOUND   = 13,
        MOUNT_POINT_NOT_FOUND     = 14,
        READ_ONLY_FILESYSTEM      = 15,
        FILE_EMPTY                = 16, //@formatter:on
    }
}
