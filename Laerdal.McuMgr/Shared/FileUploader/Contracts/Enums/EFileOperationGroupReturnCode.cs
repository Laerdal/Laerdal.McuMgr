namespace Laerdal.McuMgr.FileUploader.Contracts.Enums
{
    public enum EFileOperationGroupReturnCode //@formatter:off
    {
        Unset                  = -99,
        Ok                     = 00,
        Unknown                = 01,
        InvalidName            = 02,
        NotFound               = 03,
        IsDirectory            = 04,
        OpenFailed             = 05,
        SeekFailed             = 06,
        ReadFailed             = 07,
        TruncateFailed         = 08,
        DeleteFailed           = 09,
        WriteFailed            = 10,
        OffsetNotValid         = 11,
        OffsetLargerThanFile   = 12,
        ChecksumHashNotFound   = 13,
        MountPointNotFound     = 14,
        ReadOnlyFilesystem     = 15,
        FileEmpty              = 16, //@formatter:on
    }
}
