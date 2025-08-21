using System;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    internal interface INativeFileDownloaderProxy :
        IDisposable,
        INativeFileDownloaderCallbacksProxy,
        INativeFileDownloaderQueryableProxy,
        INativeFileDownloaderCommandableProxy
    {
    }
}