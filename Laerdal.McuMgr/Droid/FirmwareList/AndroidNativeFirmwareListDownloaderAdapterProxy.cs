
using System;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareList
{
    public class AndroidNativeFirmwareListDownloaderAdapterProxy : AndroidFirmwareListDownloader, INativeFirmwareListDownloaderProxy
    {
        // ReSharper disable once UnusedMember.Local
        private AndroidNativeFirmwareListDownloaderAdapterProxy(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        internal AndroidNativeFirmwareListDownloaderAdapterProxy(Context context, BluetoothDevice bluetoothDevice)
            : base(context, bluetoothDevice)
        {
        }

        public string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
        {
            return this.BeginDownload(initialMtuSize, (int)minimumNativeLogLevel);
        }
    }
}