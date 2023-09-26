using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    public interface IDeviceResetterQueryable
    {
        /// <summary>Returns the last fatal error message that was emitted by the device-resetter.</summary>
        string LastFatalErrorMessage { get; }

        /// <summary>Returns the current state of the device-resetter.</summary>
        EDeviceResetterState State { get; }
    }
}