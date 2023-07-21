// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;

namespace Laerdal.McuMgr.FileUploader.Events
{
    public sealed class FileUploadProgressPercentageAndDataThroughputChangedEventArgs : EventArgs
    {
        public string RemoteFilePath { get; }
        public int ProgressPercentage { get; }
        public float AverageThroughput { get; } //kbs / sec

        public FileUploadProgressPercentageAndDataThroughputChangedEventArgs(string remoteFilePath, int progressPercentage, float averageThroughput)
        {
            RemoteFilePath = remoteFilePath;
            AverageThroughput = averageThroughput;
            ProgressPercentage = progressPercentage;
        }
    }
}