using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.DeviceInformation.Contracts.Native
{
    public interface INativeDeviceInformationDownloaderProxy :
        IDisposable
    {
        string DownloadDeviceInformation(int initialMtuSize, ELogLevel minimumNativeLogLevel);
    }
}