// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        public FirmwareEraser(object bleDevice)
        {
            throw new NotImplementedException();
        }

        public string LastFatalErrorMessage => throw new NotImplementedException();

        public void Disconnect() => throw new NotImplementedException();
        public void BeginErasure(int imageIndex = 1) => throw new NotImplementedException();
    }
}
