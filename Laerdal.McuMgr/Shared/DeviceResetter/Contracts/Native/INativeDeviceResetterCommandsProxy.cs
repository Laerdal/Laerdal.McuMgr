using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterCommandsProxy
    {
        EDeviceResetterState State { get; }

        string LastFatalErrorMessage { get; }

        void Disconnect();
        void BeginReset();
    }
}
