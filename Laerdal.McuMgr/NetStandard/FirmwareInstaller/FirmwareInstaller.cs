// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
        public FirmwareInstaller(object bluetoothDevice) => throw new NotImplementedException();
    }
}
