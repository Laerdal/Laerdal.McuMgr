// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;
using System.Collections.Generic;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList.Model;

namespace Laerdal.McuMgr.FirmwareList.Contracts
{
    public interface IFirmwareListDownloader :
        IDisposable
    {
        List<InstalledFirmwareInformation> Get(
            ELogLevel? minimumNativeLogLevel = null,
            int? initialMtuSize = null
        );        
    }
}