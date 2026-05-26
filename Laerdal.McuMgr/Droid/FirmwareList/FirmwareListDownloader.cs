// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.Bluetooth;
using Android.Content;
using Android.App;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareList.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareList
{
    public partial class FirmwareListDownloader
    {

        public FirmwareListDownloader(object nativeBluetoothDevice, object androidContext = null) : this( // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            androidContext: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<Context>(obj: androidContext, parameterName: nameof(androidContext), allowNulls: true),
            bluetoothDevice: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<BluetoothDevice>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice))
        )
        {
        }

        public FirmwareListDownloader(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        {
        }

        static private INativeFirmwareListDownloaderProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
            
            androidContext ??= Application.Context;

            return new AndroidNativeFirmwareListDownloaderAdapterProxy(
                context: androidContext,
                bluetoothDevice: bluetoothDevice
            );
        }
    }
}