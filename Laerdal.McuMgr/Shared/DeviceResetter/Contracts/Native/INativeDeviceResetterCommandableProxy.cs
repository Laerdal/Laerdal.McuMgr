using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterCommandableProxy
    {
        void Disconnect();
        EDeviceResetterInitializationVerdict BeginReset();
    }
}
