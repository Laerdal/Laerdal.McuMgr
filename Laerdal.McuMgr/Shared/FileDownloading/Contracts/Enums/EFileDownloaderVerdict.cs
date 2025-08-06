using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Enums
{
    [Flags]
    public enum EFileDownloaderVerdict //this must mirror the java enum values of E[Android|iOS]FileDownloaderVerdict
    {
        Success = 0,
        FailedInvalidSettings = 1,
        FailedErrorUponCommencing = 2,
        FailedDownloadAlreadyInProgress = 3,
    }
}