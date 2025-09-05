namespace Laerdal.McuMgr.FileDownloading.Contracts.Enums
{
    public enum EFileDownloaderState //these must mirror the java enum values of EFileDownloaderState on both android and ios
    {
        None = 0,
        Idle = 1,
        Downloading = 2,
        Paused = 3,
        Resuming = 4,
        Complete = 5,
        Cancelled = 6,
        Error = 7,
        Cancelling = 8,
    }
}