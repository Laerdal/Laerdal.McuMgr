// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.DeviceInformation.Contracts
{
    public interface IDeviceInformationDownloader :
        IDisposable
    {
        /// <summary>Sets the bluetooth device.</summary>
        /// <returns>True if the bluetooth device was successfully set to the specified one - False otherwise (which typically means that an upload is still ongoing)</returns>
        bool TrySetBluetoothDevice(object bluetoothDevice);

        string DownloadAsync(
            ELogLevel? minimumNativeLogLevel = null,
            int? initialMtuSize = null
        );
    }
}