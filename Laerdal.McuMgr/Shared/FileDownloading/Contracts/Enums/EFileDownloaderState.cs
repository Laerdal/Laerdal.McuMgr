namespace Laerdal.McuMgr.FileDownloading.Contracts.Enums
{
    public enum EFileDownloaderState //these must mirror the java enum values of EFileDownloaderState on both android and ios
    {
        None = 0,
        Idle = 1,
        Downloading = 2,
        Paused = 3,
        Complete = 4,
        Cancelled = 5,
        Error = 6,
        Cancelling = 7,
    }
}