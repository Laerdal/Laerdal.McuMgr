// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Runtime.InteropServices;
using Laerdal.McuMgr.FirmwareErasure.Contracts;

namespace Laerdal.McuMgr.FirmwareErasure
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        /// <summary>This constructor is employed when using the *-force-dud nuget packages to provide dummy support for the sake of compiling stuff without issues even on unsupported platforms.</summary>
        /// <throws>Always throws <see cref="PlatformNotSupportedException"/> regardless of platform.</throws>
        public FirmwareEraser(object nativeBluetoothDevice)
            => throw new PlatformNotSupportedException($"McuMgr.{nameof(FirmwareEraser)} is not supported on your particular OS ({RuntimeInformation.OSDescription})");
    }
}
