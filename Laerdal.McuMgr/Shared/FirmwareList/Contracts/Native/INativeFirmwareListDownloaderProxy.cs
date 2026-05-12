using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareList.Contracts.Native
{
    public interface INativeFirmwareListDownloaderProxy :
        IDisposable
    {
        string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel);
    }
}