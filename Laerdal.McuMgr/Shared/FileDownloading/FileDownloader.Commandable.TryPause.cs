using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public bool TryPause()
        {
            if (IsDisposed || IsCancellationRequested || !IsOperationOngoing)
                return false;

            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Trace, message: "[FD.TPS.010] Received request to pause the ongoing download operation", category: "FileDownloader", resource: ""));

            KeepGoing.Reset(); //order                       blocks any ongoing installation/verification
            NativeFileDownloaderProxy?.TryPause(); //order   ignore the return value

            return true; //must always return true
        }
        
        protected virtual async Task CheckIfPausedOrCancelledAsync(string remoteFilePath) //used to check the 'KeepGoing' guard
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileDownloader));

            if (IsCancellationRequested)
                throw new UploadCancelledException(CancellationReason);
            
            var mustPauseHere = !KeepGoing.IsSet; //00
            if (mustPauseHere)
            {
                OnStateChanged(new(remoteFilePath: remoteFilePath, EFileDownloaderState.None, EFileDownloaderState.Paused)); //  we kinda emulate the behavior 
                OnFileDownloadPaused(new(remoteFilePath: remoteFilePath)); //                                                    of the native layer
            }
        
            await KeepGoing.WaitAsync(); //order 10

            if (IsDisposed) //order
                throw new ObjectDisposedException(nameof(FileDownloader));

            if (IsCancellationRequested)
                throw new UploadCancelledException(CancellationReason);
            
            if (mustPauseHere)
            {
                OnStateChanged(new(remoteFilePath: remoteFilePath, EFileDownloaderState.Paused, EFileDownloaderState.None)); //  we kinda emulate the behavior of the native layer
                OnFileDownloadResumed(new(remoteFilePath: remoteFilePath)); //00                                                 (we skip the 'resuming' state though)
            }
        
            //00  if the semaphore count is zero it means we are about to get paused without any ongoing
            //    transfers so we will emit the paused/resumed events here to keep things consistent
            //
            //10  we just want to check if we are paused/cancelled   this will block if we are paused
            //    immediately release again   we dont want to postpone this any longer than necessary
        }
    }
}