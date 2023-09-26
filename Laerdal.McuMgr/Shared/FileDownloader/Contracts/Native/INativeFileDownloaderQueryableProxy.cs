namespace Laerdal.McuMgr.FileDownloader.Contracts.Native
{
    internal interface INativeFileDownloaderQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}