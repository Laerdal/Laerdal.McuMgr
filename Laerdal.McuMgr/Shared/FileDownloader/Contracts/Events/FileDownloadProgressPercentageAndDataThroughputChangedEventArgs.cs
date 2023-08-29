// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileDownloader.Contracts.Events
{
    public readonly struct FileDownloadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs
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