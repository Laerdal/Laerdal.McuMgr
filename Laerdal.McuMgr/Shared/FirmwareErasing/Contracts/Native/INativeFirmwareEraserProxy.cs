using System;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Native
{
    internal interface INativeFirmwareEraserProxy :
        INativeFirmwareEraserCallbacksProxy,
        INativeFirmwareEraserQueryableProxy,
        INativeFirmwareEraserCommandableProxy,
        IDisposable
    {
    }
}