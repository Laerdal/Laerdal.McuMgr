using Laerdal.McuMgr.FileDownloader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderCommandsProxy
    {
        string LastFatalErrorMessage { get; }

        void Cancel();
        void Disconnect();
        EFileDownloaderVerdict BeginDownload(string remoteFilePath);
    }
}