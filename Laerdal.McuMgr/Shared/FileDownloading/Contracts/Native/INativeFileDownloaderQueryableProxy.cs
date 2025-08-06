namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    internal interface INativeFileDownloaderQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}