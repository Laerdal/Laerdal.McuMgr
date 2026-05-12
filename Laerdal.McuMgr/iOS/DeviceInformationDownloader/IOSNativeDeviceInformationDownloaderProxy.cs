using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.DeviceInformation.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.DeviceInformation
{
    // ReSharper disable once InconsistentNaming
    internal sealed class IOSNativeDeviceInformationDownloaderProxy : IOSListenerForDeviceInformationDownloader, INativeDeviceInformationDownloaderProxy
    {
        private readonly IOSDeviceInformationDownloader _nativeDownloader;

        internal IOSNativeDeviceInformationDownloaderProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            _nativeDownloader = new IOSDeviceInformationDownloader(cbPeripheral: bluetoothDevice, listener: this);
        }

        public string DownloadDeviceInformation(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            => _nativeDownloader.BeginDownload(
                initialMtuSize: initialMtuSize,
                minimumNativeLogLevelNumeric: (nint)(int)minimumNativeLogLevel
            );

        public new void Dispose()
        {
            try { _nativeDownloader?.NativeDispose(); } catch { /*ignored*/ }
            try { base.Dispose(); } catch { /*ignored*/ }

            GC.SuppressFinalize(this);
        }

        public override void LogMessageAdvertisement(string message, string category, string level) { }
        public override void FatalErrorOccurredAdvertisement(string errorMessage, nint globalErrorCode) { }
        public override void BusyStateChangedAdvertisement(bool busyNotIdle) { }
    }
}