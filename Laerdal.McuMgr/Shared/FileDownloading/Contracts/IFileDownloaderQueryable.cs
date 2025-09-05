namespace Laerdal.McuMgr.FileDownloading.Contracts
{
    public interface IFileDownloaderQueryable
    {
        /// <summary>Holds the last error message emitted</summary>
        string LastFatalErrorMessage { get; }
    }
}