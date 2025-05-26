// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughput; //kbs / sec

        public FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float currentThroughput)
        {
            CurrentThroughput = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: currentThroughput,
                digits: 1
            );

            ProgressPercentage = progressPercentage;
        }
    }
}
