// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        public DeviceResetter(object bleDevice)
        {
            throw new NotImplementedException();
        }

        public string LastFatalErrorMessage => throw new NotImplementedException();
        
        public IDeviceResetter.EDeviceResetterState State => throw new NotImplementedException();

        public void BeginReset() => throw new NotImplementedException();
        public void Disconnect() => throw new NotImplementedException();
    }
}
