using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    internal interface INativeFileDownloaderProxy :
        INativeFileDownloaderCallbacksProxy,
        INativeFileDownloaderQueryableProxy,
        INativeFileDownloaderCommandableProxy,
        IDisposable
    {
    }
}