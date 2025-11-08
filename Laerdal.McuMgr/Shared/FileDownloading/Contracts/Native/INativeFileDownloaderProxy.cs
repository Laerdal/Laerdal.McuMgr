using System;
using Laerdal.McuMgr.Common.Enums;

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