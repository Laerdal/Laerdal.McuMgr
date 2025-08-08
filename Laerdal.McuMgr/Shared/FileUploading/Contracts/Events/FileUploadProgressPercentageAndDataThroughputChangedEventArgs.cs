// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    //[StructLayout(LayoutKind.Sequential)] //no need both int and float are 4bytes long
    public readonly struct FileUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly string LocalResource; //filepath or similar   dud if not applicable
        
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughputInKbps; //kbs / sec
        public readonly float TotalAverageThroughputInKbps; //kbs / sec

        public FileUploadProgressPercentageAndDataThroughputChangedEventArgs(
            string localResource,
            int progressPercentage,
            float currentThroughputInKbps,
            float totalAverageThroughputInKbps
        )
        {
            // ArgumentOutOfRangeException.ThrowIfLessThan(0, progressPercentage, nameof(progressPercentage)); //nah  would just add overhead
            // ArgumentOutOfRangeException.ThrowIfGreaterThan(100, progressPercentage, nameof(progressPercentage));
            
            LocalResource = localResource ?? throw new ArgumentNullException(nameof(localResource));
            
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