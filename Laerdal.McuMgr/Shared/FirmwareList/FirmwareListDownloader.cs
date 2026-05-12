using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList.Contracts;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareList
{
    public partial class FirmwareListDownloader : IFirmwareListDownloader
    {
        private bool _disposed;
        protected readonly INativeFirmwareListDownloaderProxy NativeFirmwareListDownloaderProxy;

        public FirmwareListDownloader(INativeFirmwareListDownloaderProxy nativeFirmwareListDownloaderProxy)
        {
            NativeFirmwareListDownloaderProxy = nativeFirmwareListDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareListDownloaderProxy));
        }

        public string DownloadAsync(ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null)
            => NativeFirmwareListDownloaderProxy.DownloadFirmwareList(
                initialMtuSize: initialMtuSize ?? -1,
                minimumNativeLogLevel: minimumNativeLogLevel ?? ELogLevel.Error
            );

        public void Dispose()
        {
            Dispose(isDisposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
                return;
            
            if (!isDisposing)
                return;
            
            _disposed = true;
        }
    }
}