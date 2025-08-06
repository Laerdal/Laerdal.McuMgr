using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetting.Contracts.Native
{
    internal interface INativeDeviceResetterCommandableProxy
    {
        void Disconnect();
        EDeviceResetterInitializationVerdict BeginReset();
    }
}
