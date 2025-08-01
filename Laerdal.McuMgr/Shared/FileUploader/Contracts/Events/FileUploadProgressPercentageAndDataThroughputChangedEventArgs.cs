// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    //[StructLayout(LayoutKind.Sequential)] //no need both int and float are 4bytes long
    public readonly struct FileUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughputInKbps; //kbs / sec
        public readonly float TotalAverageThroughputInKbps; //kbs / sec

        public FileUploadProgressPercentageAndDataThroughputChangedEventArgs(int progressPercentage, float currentThroughputInKbps, float totalAverageThroughputInKbps)
        {
            ProgressPercentage = progressPercentage;
            
            CurrentThroughputInKbps = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: currentThroughputInKbps,
                digits: 1
            );

            TotalAverageThroughputInKbps = (float) Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: totalAverageThroughputInKbps,
                digits: 1
            );
        }
    }
}