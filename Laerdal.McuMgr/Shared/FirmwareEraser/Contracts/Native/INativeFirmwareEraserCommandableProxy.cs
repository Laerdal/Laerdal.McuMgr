using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Native
{
    internal interface INativeFirmwareEraserCommandableProxy
    {
        void Disconnect();
        EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex);
    }
}