using System;

namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    internal interface INativeFileUploaderProxy :
        INativeFileUploaderQueryableProxy,
        INativeFileUploaderCommandableProxy,
        INativeFileUploaderCallbacksProxy,
        INativeFileUploaderCleanupProxy,
        IDisposable
    {
    }
}