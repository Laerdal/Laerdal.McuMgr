using System;

namespace Laerdal.McuMgr.DeviceInformation.Contracts.Native
{
    public interface INativeDeviceInformationDownloaderProxy :
        IDisposable
    {
        string DownloadDeviceInformation();
    }
}