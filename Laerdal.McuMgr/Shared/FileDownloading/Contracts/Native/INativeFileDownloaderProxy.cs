using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    public interface INativeFileDownloaderProxy :
        IDisposable,
        INativeFileDownloaderCallbacksProxy,
        INativeFileDownloaderQueryableProxy,
        INativeFileDownloaderCommandableProxy
    {
    }
}