namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    public interface INativeFileDownloaderQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}