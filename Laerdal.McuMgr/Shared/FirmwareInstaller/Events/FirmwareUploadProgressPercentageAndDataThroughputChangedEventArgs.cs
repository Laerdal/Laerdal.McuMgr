// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FirmwareInstaller.Events
{
    public sealed class FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public float AverageThroughput { get; } //kbs / sec

        public FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float averageThroughput)
        {
            AverageThroughput = averageThroughput;
            ProgressPercentage = progressPercentage;
        }
    }
}