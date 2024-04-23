using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderProxy :
        INativeFileDownloaderQueryableProxy,
        INativeFileDownloaderCommandableProxy,
        INativeFileDownloaderCallbacksProxy,
        IDisposable
    {
    }
}