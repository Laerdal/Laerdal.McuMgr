using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    [Flags]
    public enum EFileDownloaderVerdict //this must mirror the java enum values of E[Android|iOS]FileDownloaderVerdict
    {
        Success = 0,
        FailedInvalidSettings = 1,
        FailedDownloadAlreadyInProgress = 2,
    }
}