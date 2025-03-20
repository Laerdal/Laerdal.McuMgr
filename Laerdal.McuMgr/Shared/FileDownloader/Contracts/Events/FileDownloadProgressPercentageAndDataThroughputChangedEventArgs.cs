// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct FileDownloadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs
    {
        public int ProgressPercentage { get; }
        public float CurrentThroughput { get; } //kbs / sec

        public FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float currentThroughput)
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
