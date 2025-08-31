// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Native;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFileUploaderProxy
        internal class GenericNativeFileUploaderCallbacksProxy : INativeFileUploaderCallbacksProxy
        {
            public IFileUploaderEventEmittable FileUploader { get; set; }

            public void CancelledAdvertisement(string reason = "")
                => FileUploader?.OnCancelled(new CancelledEventArgs(reason));
            
            public void CancellingAdvertisement(string reason = "")
                => FileUploader?.OnCancelling(new CancellingEventArgs(reason));

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resourceId)
                => FileUploader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: resourceId
                ));

            public void StateChangedAdvertisement(string resourceId, string remoteFilePath, EFileUploaderState oldState, EFileUploaderState newState, long totalBytesToBeUploaded)
            {                
                FileUploader?.OnStateChanged(new StateChangedEventArgs(newState: newState, oldState: oldState, resourceId: resourceId, remoteFilePath: remoteFilePath)); //keep first

                switch (newState) //keep second
                {
                    case EFileUploaderState.None: // * -> none
                        FileUploader?.OnFileUploadProgressPercentageAndDataThroughputChanged(new(resourceId: resourceId, remoteFilePath: remoteFilePath, progressPercentage: 0, currentThroughputInKBps: 0, totalAverageThroughputInKBps: 0));
                        break;
                    case EFileUploaderState.Paused: // * -> paused
                        FileUploader?.OnFileUploadPaused(new(resourceId: resourceId, remoteFilePath: remoteFilePath));
                        break;
                    case EFileUploaderState.Uploading: // idle/resuming -> uploading
                        if (oldState is not EFileUploaderState.Idle and not EFileUploaderState.Resuming)
                        {
                            FileUploader?.OnLogEmitted(new(level: ELogLevel.Warning, message: $"[FU.SCA.010] State changed to '{EFileUploaderState.Uploading}' from an unexpected state '{oldState}' - this looks fishy so report this incident!", category: "FileUploader", resource: resourceId));
                        }

                        if (oldState == EFileUploaderState.Resuming)
                        {
                            FileUploader?.OnFileUploadResumed(new(resourceId: resourceId, remoteFilePath: remoteFilePath)); //30
                        }
                        else // != resuming means we it just started
                        {
                            FileUploader?.OnFileUploadStarted(new(resourceId: resourceId, remoteFilePath: remoteFilePath, totalBytesToBeUploaded: totalBytesToBeUploaded)); //30
                        }

                        break;
                    case EFileUploaderState.Complete: // idle/uploading/paused/resuming -> complete
                        if (oldState != EFileUploaderState.Uploading) //00
                        {
                            FileUploader?.OnLogEmitted(new(level: ELogLevel.Warning, message: $"[FU.SCA.050] State changed to 'complete' from an unexpected state '{oldState}' - this looks fishy so report this incident!", category: "FileUploader", resource: resourceId));
                        }

                        if (oldState is EFileUploaderState.Paused or EFileUploaderState.Resuming) // resuming/paused -> complete   (very rare cornercase)
                        {
                            FileUploader?.OnFileUploadResumed(new(resourceId: resourceId, remoteFilePath: remoteFilePath)); //workaround
                        }

                        FileUploader?.OnFileUploadProgressPercentageAndDataThroughputChanged(new(resourceId: resourceId, remoteFilePath: remoteFilePath, progressPercentage: 100, currentThroughputInKBps: 0, totalAverageThroughputInKBps: 0)); //50
                        FileUploader?.OnFileUploadCompleted(new(resourceId: resourceId, remoteFilePath: remoteFilePath));
                        break;

                    //default: break; // idle error paused resuming cancelled cancelling    these have their own dedicated advertisements so we ignore them here    
                }

                //30   we took a conscious decision to have separate events for upload-started/paused/resumed/completed and not to try to cram everything into
                //     the state-changed event   this is out of respect for the notion of separation-of-concerns
                //
                //50   trivial hotfix to deal with the fact that the file-upload progress% doesnt fill up to 100%
                //
                //90   note that tiny files which are only a few bytes long can go idle->complete in a heartbeat skipping the 'uploading' state altogether
            }

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FileUploader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(
                string resourceId,
                string remoteFilePath,
                string errorMessage,
                EGlobalErrorCode globalErrorCode
            ) => FileUploader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,

                errorMessage: errorMessage,
                globalErrorCode: globalErrorCode
            ));

            public void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                string resourceId,
                string remoteFilePath,
                int progressPercentage,
                float currentThroughputInKBps,
                float totalAverageThroughputInKBps
            ) => FileUploader?.OnFileUploadProgressPercentageAndDataThroughputChanged(new FileUploadProgressPercentageAndDataThroughputChangedEventArgs(
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,
                progressPercentage: progressPercentage,
                currentThroughputInKBps: currentThroughputInKBps,
                totalAverageThroughputInKBps: totalAverageThroughputInKBps
            ));
        }
    }
}
