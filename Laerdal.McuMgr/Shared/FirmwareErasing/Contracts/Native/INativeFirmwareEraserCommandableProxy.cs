using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Native
{
    internal interface INativeFirmwareEraserCommandableProxy
    {
        void Disconnect();
        EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex);
    }
}