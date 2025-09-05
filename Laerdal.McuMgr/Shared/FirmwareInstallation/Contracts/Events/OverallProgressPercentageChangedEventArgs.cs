using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Events
{
    //[StructLayout(LayoutKind.Sequential)] //no need both int and float are 4bytes long
    public readonly struct OverallProgressPercentageChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly int ProgressPercentage;

        public OverallProgressPercentageChangedEventArgs(int progressPercentage)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(progressPercentage, nameof(progressPercentage));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(progressPercentage, 100, nameof(progressPercentage));
            
            ProgressPercentage = progressPercentage;
        }
    }
}