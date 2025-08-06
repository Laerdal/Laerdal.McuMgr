namespace Laerdal.McuMgr.DeviceResetting.Contracts.Native
{
    internal interface INativeDeviceResetterProxy :
        INativeDeviceResetterQueryableProxy,
        INativeDeviceResetterCommandableProxy,
        INativeDeviceResetterCallbacksProxy
    {
    }
}
 