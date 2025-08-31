namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryPause() => _nativeFileDownloaderProxy?.TryPause() ?? false;
    }
}