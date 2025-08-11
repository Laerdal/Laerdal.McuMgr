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
        public readonly float CurrentThroughputInKbps; //      kbs / sec
        public readonly float TotalAverageThroughputInKbps; // kbs / sec

        public FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(string resourceId, int progressPercentage, float currentThroughputInKbps, float totalAverageThroughputInKbps)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(currentThroughputInKbps, nameof(totalAverageThroughputInKbps));
            ArgumentOutOfRangeException.ThrowIfNegative(totalAverageThroughputInKbps, nameof(totalAverageThroughputInKbps));
            
            ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
            ProgressPercentage = progressPercentage is >= 0 and <= 100
                ? progressPercentage
                : throw new ArgumentOutOfRangeException(nameof(progressPercentage), "Progress percentage must be between 0 and 100.");
            
            CurrentThroughputInKbps = RoundThroughput_(currentThroughputInKbps);
            TotalAverageThroughputInKbps = RoundThroughput_(totalAverageThroughputInKbps);
            return;

            static float RoundThroughput_(float value_) => (float) Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: value_,
                digits: 1
            );
        }
    }
}
