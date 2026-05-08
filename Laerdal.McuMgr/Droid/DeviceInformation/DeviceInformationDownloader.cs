// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.Bluetooth;
using Android.Content;
using Android.App;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.DeviceInformation.Contracts.Native;

namespace Laerdal.McuMgr.DeviceInformation
{
    public partial class DeviceInformationDownloader
    {

        public DeviceInformationDownloader(object nativeBluetoothDevice, object androidContext = null) : this( // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            androidContext: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<Context>(obj: androidContext, parameterName: nameof(androidContext), allowNulls: true),
            bluetoothDevice: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<BluetoothDevice>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice))
        )
        {
        }

        public DeviceInformationDownloader(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        {
        }

        static private INativeDeviceInformationDownloaderProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
            
            androidContext ??= Application.Context;

            return new AndroidNativeDeviceInformationDownloaderAdapterProxy(
                context: androidContext,
                bluetoothDevice: bluetoothDevice
            );
        }
    }
}