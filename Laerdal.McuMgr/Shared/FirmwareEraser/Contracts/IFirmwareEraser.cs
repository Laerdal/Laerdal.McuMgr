// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraser : IFirmwareEraserEventSubscribable, IFirmwareEraserCommandable // dont add IFirmwareEraserEventEmittable here   its supposed to be internal only
    {
    }
}
