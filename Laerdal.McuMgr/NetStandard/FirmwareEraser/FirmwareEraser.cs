// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.FirmwareEraser.Contracts;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        public FirmwareEraser(object bluetoothDevice) => throw new NotImplementedException();
    }
}
