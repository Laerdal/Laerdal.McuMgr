using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetting.Contracts
{
    public interface IDeviceResetterQueryable
    {
        /// <summary>Returns the last fatal error message that was emitted by the device-resetter.</summary>
        string LastFatalErrorMessage { get; }

        /// <summary>Returns the current state of the device-resetter.</summary>
        EDeviceResetterState State { get; }
    }
}