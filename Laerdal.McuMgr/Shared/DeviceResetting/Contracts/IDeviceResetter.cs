// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Laerdal.McuMgr.DeviceResetting.Contracts
{
    public interface IDeviceResetter : IDeviceResetterCommandable, IDeviceResetterQueryable, IDeviceResetterEventSubscribable
    {
    }
}
