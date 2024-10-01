using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderProxy :
        INativeFileDownloaderCallbacksProxy,
        INativeFileDownloaderQueryableProxy,
        INativeFileDownloaderCommandableProxy,
        IDisposable
    {
    }
}