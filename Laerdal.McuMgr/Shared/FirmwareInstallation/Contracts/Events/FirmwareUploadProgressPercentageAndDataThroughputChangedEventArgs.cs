// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Events
{
    public readonly struct FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughputInKBps; //kbs / sec
        public readonly float TotalAverageThroughputInKBps; //kbs / sec

        public FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
        {
            CurrentThroughputInKBps = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: currentThroughputInKBps,
                digits: 1
            );
            
            TotalAverageThroughputInKBps = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: totalAverageThroughputInKBps,
                digits: 1
            );

            ProgressPercentage = progressPercentage;
        }
    }
}
