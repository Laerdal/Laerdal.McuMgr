using System;
using System.Runtime.InteropServices;

namespace Laerdal.McuMgr.DeviceInformation
{
    public partial class DeviceInformationDownloader
    {
        /// <summary>This constructor is employed when using the *-force-dud nuget packages to provide dummy support for the sake of compiling stuff without issues even on unsupported platforms.</summary>
        /// <throws>Always throws <see cref="PlatformNotSupportedException"/> regardless of platform.</throws>
        public DeviceInformationDownloader(object nativeBluetoothDevice)
            => throw new PlatformNotSupportedException($"McuMgr.{nameof(DeviceInformationDownloader)} is not supported on your particular OS ({RuntimeInformation.OSDescription})");
    }
}