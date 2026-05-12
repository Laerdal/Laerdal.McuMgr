// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareList.Contracts
{
    public interface IFirmwareListDownloader :
        IDisposable
    {
        string DownloadAsync(
            ELogLevel? minimumNativeLogLevel = null,
            int? initialMtuSize = null
        );
    }
}