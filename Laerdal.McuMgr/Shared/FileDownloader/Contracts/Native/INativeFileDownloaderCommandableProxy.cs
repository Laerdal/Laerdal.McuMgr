﻿using Laerdal.McuMgr.FileDownloader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderCommandableProxy
    {
        void Cancel();
        void Disconnect();
        EFileDownloaderVerdict BeginDownload(string remoteFilePath);
    }
}