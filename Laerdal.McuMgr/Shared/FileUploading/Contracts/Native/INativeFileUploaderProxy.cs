using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    public interface INativeFileUploaderProxy :
        IDisposable,
        INativeFileUploaderCleanupProxy,
        INativeFileUploaderCallbacksProxy,
        INativeFileUploaderQueryableProxy,
        INativeFileUploaderCommandableProxy
    {
    }
}