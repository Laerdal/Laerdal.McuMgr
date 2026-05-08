using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.DeviceInformation.Contracts;
using Laerdal.McuMgr.DeviceInformation.Contracts.Native;

namespace Laerdal.McuMgr.DeviceInformation
{
    public partial class DeviceInformationDownloader : IDeviceInformationDownloader
    {
        private bool _disposed;
        protected readonly INativeDeviceInformationDownloaderProxy NativeDeviceInformationDownloaderProxy;

        public DeviceInformationDownloader(INativeDeviceInformationDownloaderProxy nativeDeviceInformationDownloaderProxy)
        {
            NativeDeviceInformationDownloaderProxy = nativeDeviceInformationDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeDeviceInformationDownloaderProxy));
        }

        public string DownloadAsync(ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null)
            => NativeDeviceInformationDownloaderProxy.DownloadDeviceInformation(
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