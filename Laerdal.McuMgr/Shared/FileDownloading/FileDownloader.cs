using System;
using Laerdal.McuMgr.Common.AsyncX;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.FileDownloading
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader, IFileDownloaderEventEmittable
    {
        protected bool IsOperationOngoing;
        protected bool IsCancellationRequested;
        protected string CancellationReason = "";
        protected readonly object OperationCheckLock = new();
        
        protected readonly AsyncManualResetEvent KeepGoing = new(set: true); //related to pausing/unpausing   keepgoing=true by default
        
        protected readonly INativeFileDownloaderProxy NativeFileDownloaderProxy;
        
        public string LastFatalErrorMessage => NativeFileDownloaderProxy?.LastFatalErrorMessage;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileDownloaderProxy
        internal FileDownloader(INativeFileDownloaderProxy nativeFileDownloaderProxy)
        {
            NativeFileDownloaderProxy = nativeFileDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderProxy));
            NativeFileDownloaderProxy.FileDownloader = this; //vital
        }

        protected virtual void ResetInternalStateTidbits()
        {
            //IsOperationOngoing = false; //dont

            CancellationReason = "";
            IsCancellationRequested = false;

            KeepGoing.Set(); // unblocks any ongoing installation/verification    just in case
        }
    }
}