using System;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Native
{
    internal interface INativeFirmwareEraserProxy :
        INativeFirmwareEraserCallbacksProxy,
        INativeFirmwareEraserQueryableProxy,
        INativeFirmwareEraserCommandableProxy,
        IDisposable
    {
    }
}