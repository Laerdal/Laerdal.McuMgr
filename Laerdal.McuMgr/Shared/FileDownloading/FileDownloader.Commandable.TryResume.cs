namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryResume() => _nativeFileDownloaderProxy?.TryResume() ?? false;
    }
}