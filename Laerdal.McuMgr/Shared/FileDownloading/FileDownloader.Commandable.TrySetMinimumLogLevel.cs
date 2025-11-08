using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TrySetMinimumLogLevel(ELogLevel minimumLogLevel)
        {
            return NativeFileDownloaderProxy?.TrySetMinimumLogLevel(minimumLogLevel) ?? true;
        }
    }
}

