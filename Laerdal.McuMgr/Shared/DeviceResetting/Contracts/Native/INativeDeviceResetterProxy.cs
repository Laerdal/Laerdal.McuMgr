using System;

namespace Laerdal.McuMgr.DeviceResetting.Contracts.Native
{
    internal interface INativeDeviceResetterProxy :
        INativeDeviceResetterQueryableProxy,
        INativeDeviceResetterCommandableProxy,
        INativeDeviceResetterCallbacksProxy,
        IDisposable
    {
    }
}
 