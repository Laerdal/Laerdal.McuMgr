using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetting.Contracts.Native
{
    internal interface INativeDeviceResetterQueryableProxy
    {
        EDeviceResetterState State { get; }

        string LastFatalErrorMessage { get; }
    }
}