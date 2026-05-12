using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FirmwareList
{
    // ReSharper disable once InconsistentNaming
    internal sealed class IOSNativeFirmwareListDownloaderProxy : IOSListenerForFirmwareListDownloader, INativeFirmwareListDownloaderProxy
    {
        private readonly IOSFirmwareListDownloader _nativeDownloader;

        internal IOSNativeFirmwareListDownloaderProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            _nativeDownloader = new IOSFirmwareListDownloader(cbPeripheral: bluetoothDevice, listener: this);
        }

        public string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
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