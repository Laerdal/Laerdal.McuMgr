namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    internal interface INativeDeviceResetterCommandsProxy
    {
        object State { get; }

        string LastFatalErrorMessage { get; }

        void Disconnect();
        void BeginReset();
    }
}
