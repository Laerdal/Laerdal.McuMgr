﻿using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Native
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