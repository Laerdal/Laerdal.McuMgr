// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.DeviceInformation.Contracts
{
    public interface IDeviceInformationDownloader :
        IDisposable
    {
        string DownloadAsync(
            ELogLevel? minimumNativeLogLevel = null,
            int? initialMtuSize = null
        );
    }
}