using System;
using System.Collections.Generic;
using System.Text.Json;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList.Contracts;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;
using Laerdal.McuMgr.FirmwareList.Model;

namespace Laerdal.McuMgr.FirmwareList
{
    public partial class FirmwareListDownloader : IFirmwareListDownloader
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private bool _disposed;
        protected readonly INativeFirmwareListDownloaderProxy NativeFirmwareListDownloaderProxy;

        internal FirmwareListDownloader(INativeFirmwareListDownloaderProxy nativeFirmwareListDownloaderProxy)
        {
            NativeFirmwareListDownloaderProxy = nativeFirmwareListDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareListDownloaderProxy));
        }

        public List<InstalledFirmwareInformation> Get(ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null)
        {
            var jsonData = NativeFirmwareListDownloaderProxy.Get(
                initialMtuSize: initialMtuSize ?? -1,
                minimumNativeLogLevel: minimumNativeLogLevel ?? ELogLevel.Error
            );

            return JsonSerializer.Deserialize<List<InstalledFirmwareInformation>>(jsonData, _jsonSerializerOptions);
        }

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


            try
            {
                NativeFirmwareListDownloaderProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            _disposed = true;
        }
    }
}