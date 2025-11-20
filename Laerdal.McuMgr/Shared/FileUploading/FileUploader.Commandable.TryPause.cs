// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TryPause()
        {
            if (IsDisposed || IsCancellationRequested || !IsOperationOngoing)
                return false;

            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Trace, message: "[FU.TPS.010] Received request to pause the ongoing upload operation", category: "FileUploader", resource: ""));

            KeepGoing.Reset(); //order                     blocks any ongoing installation/verification
            NativeFileUploaderProxy?.TryPause(); //order   ignore the return value

            return true; //must always return true
        }
        
        protected virtual async Task CheckIfPausedOrCancelledAsync(string resourceId, string remoteFilePath) //used to check the 'KeepGoing' guard
        {            
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileUploader));

            if (IsCancellationRequested)
                throw new UploadCancelledException(CancellationReason);
            
            var mustPauseHere = !KeepGoing.IsSet; //00
            if (mustPauseHere)
            {
                OnStateChanged(new(resourceId: resourceId, remoteFilePath: remoteFilePath, EFileUploaderState.None, EFileUploaderState.Paused)); //  we kinda emulate the behavior 
                OnFileUploadPaused(new(resourceId: resourceId, remoteFilePath: remoteFilePath)); //                                                  of the native layer
            }
        
            await KeepGoing.WaitAsync().ConfigureAwait(false); //10
            
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(FileUploader));

            if (IsCancellationRequested)
                throw new UploadCancelledException(CancellationReason);
            
            if (mustPauseHere)
            {
                OnStateChanged(new(resourceId: resourceId, remoteFilePath: remoteFilePath, EFileUploaderState.Paused, EFileUploaderState.None)); //  we kinda emulate the behavior of the native layer
                OnFileUploadResumed(new(resourceId: resourceId, remoteFilePath: remoteFilePath)); //00                                               (we skip the 'resuming' state though)
            }
        
            //00  if the semaphore count is zero it means we are about to get paused without any ongoing
            //    transfers so we will emit the paused/resumed events here to keep things consistent
            //
            //10  we just want to check if we are paused/cancelled   this will block if we are paused
            //    immediately   we dont want to postpone this any longer than necessary
        }
    }
}
