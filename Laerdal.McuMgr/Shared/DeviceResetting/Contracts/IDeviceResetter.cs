// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;

namespace Laerdal.McuMgr.DeviceResetting.Contracts
{
    public interface IDeviceResetter :
        IDeviceResetterCommandable,
        IDeviceResetterQueryable,
        IDeviceResetterEventSubscribable,
        IDisposable
    {
    }
}
