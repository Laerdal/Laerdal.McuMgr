// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Events
{
    public readonly struct FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs
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