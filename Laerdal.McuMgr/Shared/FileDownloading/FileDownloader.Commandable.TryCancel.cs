namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryCancel(string reason = "") => NativeFileDownloaderProxy?.TryCancel(reason) ?? false;
    }
}