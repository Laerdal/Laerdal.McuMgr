
using System;
using Laerdal.McuMgr.Bindings.Android;
using Laerdal.McuMgr.DeviceInformation.Contracts.Native;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.DeviceInformation
{
    public class AndroidNativeDeviceInformationDownloaderAdapterProxy : AndroidDeviceInformationDownloader, INativeDeviceInformationDownloaderProxy
    {
        // ReSharper disable once UnusedMember.Local
        private AndroidNativeDeviceInformationDownloaderAdapterProxy(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        internal AndroidNativeDeviceInformationDownloaderAdapterProxy(Context context, BluetoothDevice bluetoothDevice)
            : base(context, bluetoothDevice)
        {
        }

        public string DownloadDeviceInformation(int initialMtuSize, ELogLevel minimumNativeLogLevel)
        {
            return this.BeginDownload(initialMtuSize, (int)minimumNativeLogLevel);
        }
    }
}