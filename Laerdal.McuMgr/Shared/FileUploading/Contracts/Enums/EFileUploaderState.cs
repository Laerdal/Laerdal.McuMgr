namespace Laerdal.McuMgr.FileUploading.Contracts.Enums
{
    public enum EFileUploaderState //these must mirror the java enum values of EFileUploaderState on both android and ios
    {
        None = 0,
        Idle = 1,
        Uploading = 2,
        Paused = 3,
        Resuming = 4,
        Complete = 5,
        Cancelled = 6,
        Error = 7,
        Cancelling = 8,
    }
}