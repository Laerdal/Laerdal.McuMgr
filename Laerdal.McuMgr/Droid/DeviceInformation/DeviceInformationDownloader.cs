// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Bindings.Android;
using System.Net.Mime;
using Android.App;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.DeviceInformation
{
    public partial class DeviceInformationDownloader
    {
        public bool TrySetBluetoothDevice(object bluetoothDevice)
        {
            var androidBluetoothDevice = bluetoothDevice as BluetoothDevice ?? throw new ArgumentException($"Expected {nameof(BluetoothDevice)} to be an AndroidBluetoothDevice but got '{bluetoothDevice?.GetType().Name ?? "null"}' instead", nameof(bluetoothDevice));
                
            return base.TrySetBluetoothDevice(androidBluetoothDevice);
        }

        public string DownloadAsync(ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null)
        {
            throw new NotImplementedException();
        }
    }
}