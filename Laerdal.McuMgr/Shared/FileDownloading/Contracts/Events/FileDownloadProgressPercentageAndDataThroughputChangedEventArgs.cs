// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Events
{
    public readonly struct FileDownloadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly string ResourceId;
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughputInKBps; //      kbs / sec
        public readonly float TotalAverageThroughputInKBps; // kbs / sec

        public FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(string resourceId, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(currentThroughputInKBps, nameof(totalAverageThroughputInKBps));
            ArgumentOutOfRangeException.ThrowIfNegative(totalAverageThroughputInKBps, nameof(totalAverageThroughputInKBps));
            
            ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
            ProgressPercentage = progressPercentage is >= 0 and <= 100
                ? progressPercentage
                : throw new ArgumentOutOfRangeException(nameof(progressPercentage), "Progress percentage must be between 0 and 100.");
            
            CurrentThroughputInKBps = RoundThroughput_(currentThroughputInKBps);
            TotalAverageThroughputInKBps = RoundThroughput_(totalAverageThroughputInKBps);
            return;

            static float RoundThroughput_(float value_) => (float) Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: value_,
                digits: 1
            );
        }
    }
}
