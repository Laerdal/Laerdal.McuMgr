namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterProxy :
        INativeDeviceResetterQueryableProxy,
        INativeDeviceResetterCommandableProxy,
        INativeDeviceResetterCallbacksProxy
    {
    }
}
 