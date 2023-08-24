namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Native
{
    internal interface INativeFirmwareEraserCommandableProxy
    {
        void Disconnect();
        void BeginErasure(int imageIndex);
    }
}