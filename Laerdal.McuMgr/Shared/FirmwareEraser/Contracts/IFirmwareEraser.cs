// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraser : IFirmwareEraserEventSubscribable, IFirmwareEraserCommands // dont add IFirmwareEraserEventEmitters here   its supposed to be internal only
    {
    }
}
