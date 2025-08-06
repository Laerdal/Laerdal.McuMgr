// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Events
{
    public readonly struct FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughputInKbps; //kbs / sec
        public readonly float TotalAverageThroughputInKbps; //kbs / sec

        public FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float currentThroughputInKbps, float totalAverageThroughputInKbps)
        {
            CurrentThroughputInKbps = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: currentThroughputInKbps,
                digits: 1
            );
            
            TotalAverageThroughputInKbps = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: totalAverageThroughputInKbps,
                digits: 1
            );

            ProgressPercentage = progressPercentage;
        }
    }
}
