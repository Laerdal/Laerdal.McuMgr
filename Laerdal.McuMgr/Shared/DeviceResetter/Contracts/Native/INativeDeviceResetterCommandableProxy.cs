namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterCommandableProxy
    {
        void Disconnect();
        void BeginReset();
    }
}
