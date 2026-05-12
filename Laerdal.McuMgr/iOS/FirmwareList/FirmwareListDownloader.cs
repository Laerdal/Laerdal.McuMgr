// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareList
{
    public partial class FirmwareListDownloader
    {
        public FirmwareListDownloader(object nativeBluetoothDevice) // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            : this(NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<CBPeripheral>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice)))
        {
        }

        public FirmwareListDownloader(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }

        static private INativeFirmwareListDownloaderProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeFirmwareListDownloaderProxy(bluetoothDevice);
        }
    }
}