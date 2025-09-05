// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    //[StructLayout(LayoutKind.Sequential)] //no need both int and float are 4bytes long
    public readonly struct FileUploadProgressPercentageAndDataThroughputChangedEventArgs : IMcuMgrEventArgs //hotpath
    {
        public readonly string ResourceId; //filepath or similar   dud if not applicable
        public readonly string RemoteFilePath;
        
        public readonly int ProgressPercentage;
        public readonly float CurrentThroughputInKBps; //kbs / sec
        public readonly float TotalAverageThroughputInKBps; //kbs / sec

        public FileUploadProgressPercentageAndDataThroughputChangedEventArgs(
            string resourceId,
            string remoteFilePath,
            int progressPercentage,
            float currentThroughputInKBps,
            float totalAverageThroughputInKBps
        )
        {
            // ArgumentOutOfRangeException.ThrowIfLessThan(0, progressPercentage, nameof(progressPercentage)); //nah  would just add overhead
            // ArgumentOutOfRangeException.ThrowIfGreaterThan(100, progressPercentage, nameof(progressPercentage));
            
            ResourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));
            RemoteFilePath = remoteFilePath ?? throw new ArgumentNullException(nameof(remoteFilePath));
            
            ProgressPercentage = progressPercentage;
            
            CurrentThroughputInKBps = (float)Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: currentThroughputInKBps,
                digits: 1
            );

            TotalAverageThroughputInKBps = (float) Math.Round( //14.1999 -> 14.2
                mode: MidpointRounding.AwayFromZero,
                value: totalAverageThroughputInKBps,
                digits: 1
            );
        }
    }
}