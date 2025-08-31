namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryPause() => NativeFileDownloaderProxy?.TryPause() ?? false;
    }
}