// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.DeviceResetter.Contracts;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        public DeviceResetter(object bluetoothDevice)
        {
            throw new NotImplementedException();
        }
        
        public EDeviceResetterState State => throw new NotImplementedException();
    }
}
