namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryCancel(string reason = "")
        {
            IsCancellationRequested = true; //order

            var success = NativeFileDownloaderProxy?.TryCancel(reason) ?? false; //order

            KeepGoing.Set(); //order   unblocks any ongoing installation/verification so that it can observe the cancellation

            return success;
        }
    }
}