namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Native
{
    internal interface INativeFirmwareEraserCommandsProxy
    {
        // ReSharper disable UnusedMember.Global
        string LastFatalErrorMessage { get; }

        void Disconnect();
        void BeginErasure(int imageIndex);
    }
}