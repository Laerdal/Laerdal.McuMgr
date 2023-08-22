namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterCommandsProxy
    {
        object State { get; }

        string LastFatalErrorMessage { get; }

        void Disconnect();
        void BeginReset();
    }
}
