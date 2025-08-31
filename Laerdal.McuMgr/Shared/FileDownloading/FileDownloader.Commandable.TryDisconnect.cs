namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryDisconnect() => NativeFileDownloaderProxy?.TryDisconnect() ?? false;
    }
}