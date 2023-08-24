using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterQueryableProxy
    {
        EDeviceResetterState State { get; }

        string LastFatalErrorMessage { get; }
    }
}