namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryCancel(string reason = "") => _nativeFileDownloaderProxy?.TryCancel(reason) ?? false;
    }
}