// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    public interface IFirmwareEraser : IFirmwareEraserEventSubscribable, IFirmwareEraserCommandable, IDisposable // dont add IFirmwareEraserEventEmittable here   its supposed to be internal only
    {
    }
}
