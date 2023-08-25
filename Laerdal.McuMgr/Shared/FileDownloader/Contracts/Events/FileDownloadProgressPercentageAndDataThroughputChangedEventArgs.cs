// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public sealed class FileDownloadProgressPercentageAndDataThroughputChangedEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public float AverageThroughput { get; } //kbs / sec

        public FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float averageThroughput)
        {
            AverageThroughput = averageThroughput;
            ProgressPercentage = progressPercentage;
        }
    }
}