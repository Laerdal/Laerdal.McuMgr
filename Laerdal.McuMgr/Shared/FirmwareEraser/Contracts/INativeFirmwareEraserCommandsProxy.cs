namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface INativeFirmwareEraserCommandsProxy
    {
        // ReSharper disable UnusedMember.Global
        string LastFatalErrorMessage { get; }

        void Disconnect();
        void BeginErasure(int imageIndex);
    }
}