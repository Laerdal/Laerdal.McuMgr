using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    internal interface INativeFileUploaderProxy :
        IDisposable,
        INativeFileUploaderCleanupProxy,
        INativeFileUploaderCallbacksProxy,
        INativeFileUploaderQueryableProxy,
        INativeFileUploaderCommandableProxy
    {
    }
}