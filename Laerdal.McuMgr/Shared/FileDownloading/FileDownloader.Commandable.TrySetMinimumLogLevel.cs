using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TrySetMinimumNativeLogLevel(ELogLevel minimumNativeLogLevel)
        {
            return NativeFileDownloaderProxy?.TrySetMinimumNativeLogLevel(minimumNativeLogLevel) ?? true;
        }
    }
}

