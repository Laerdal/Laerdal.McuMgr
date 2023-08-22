namespace Laerdal.McuMgr.FileUploader.Contracts.Enums
{
    public enum EFileUploaderState //these must mirror the java enum values of EFileUploaderState on both android and ios
    {
        None = 0,
        Idle = 1,
        Uploading = 2,
        Paused = 3,
        Complete = 4,
        Cancelled = 5,
        Error = 6,
        Cancelling = 7,
    }
}