// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.DeviceInformation.Contracts.Native;

namespace Laerdal.McuMgr.DeviceInformation
{
    public partial class DeviceInformationDownloader
    {
        public DeviceInformationDownloader(object nativeBluetoothDevice) // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            : this(NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<CBPeripheral>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice)))
        {
        }

        public DeviceInformationDownloader(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }

        static private INativeDeviceInformationDownloaderProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeDeviceInformationDownloaderProxy(bluetoothDevice);
        }
    }
}