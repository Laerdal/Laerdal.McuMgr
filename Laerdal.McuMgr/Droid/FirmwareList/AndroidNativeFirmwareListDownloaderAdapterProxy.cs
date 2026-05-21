using System;
using Laerdal.McuMgr.Bindings.Android;
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
            var result = GetImageListJson(initialMtuSize, (int)minimumNativeLogLevel);

            if (result.Verdict != EAndroidFirmwareListDownloaderVerdict.Success)
                throw new InvalidOperationException($"[AndroidNativeFirmwareListDownloaderAdapterProxy.DownloadFirmwareList] Native call failed with verdict '{result.Verdict}':\n\n{result.PossibleErrorMessage}");

            return result.JsonData;
        }
    }
}