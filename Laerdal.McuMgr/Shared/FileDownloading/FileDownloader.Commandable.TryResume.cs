namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryResume() => NativeFileDownloaderProxy?.TryResume() ?? false;
    }
}