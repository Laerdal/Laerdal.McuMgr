namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryDisconnect() => _nativeFileDownloaderProxy?.TryDisconnect() ?? false;
    }
}