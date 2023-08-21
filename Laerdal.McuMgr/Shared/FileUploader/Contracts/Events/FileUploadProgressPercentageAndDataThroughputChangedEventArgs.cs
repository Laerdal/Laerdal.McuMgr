// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public sealed class FileUploadProgressPercentageAndDataThroughputChangedEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public float AverageThroughput { get; } //kbs / sec

        public FileUploadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float averageThroughput)
        {
            AverageThroughput = averageThroughput;
            ProgressPercentage = progressPercentage;
        }
    }
}