namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    internal interface INativeFileDownloaderCommandsProxy
    {
        string RemoteFilePath { get; set; }
        string LastFatalErrorMessage { get; }

        void Cancel();
        void Disconnect();
        EFileDownloaderVerdict BeginDownload(string remoteFilePath);
    }
}