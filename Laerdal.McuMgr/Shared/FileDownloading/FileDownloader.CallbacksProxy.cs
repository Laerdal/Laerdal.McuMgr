using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFileDownloaderProxy
        internal class GenericNativeFileDownloaderCallbacksProxy : INativeFileDownloaderCallbacksProxy
        {
            public IFileDownloaderEventEmittable FileDownloader { get; set; }

            public void CancelledAdvertisement(string reason)
                => FileDownloader?.OnCancelled(new CancelledEventArgs(reason));

            public void CancellingAdvertisement(string reason)
                => FileDownloader?.OnCancelling(new CancellingEventArgs(reason));

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => FileDownloader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                ));

            public void StateChangedAdvertisement(string remoteFilePath, EFileDownloaderState oldState, EFileDownloaderState newState, long totalBytesToBeDownloaded, byte[] completeDownloadedData)
            {
                FileDownloader?.OnStateChanged(new(newState: newState, oldState: oldState, remoteFilePath: remoteFilePath)); //keep first

                switch (newState) //keep second
                {
                    case EFileDownloaderState.None: // * -> none
                        FileDownloader?.OnFileDownloadProgressPercentageAndDataThroughputChanged(new(remoteFilePath: remoteFilePath, progressPercentage: 0, currentThroughputInKBps: 0, totalAverageThroughputInKBps: 0));
                        break;
                    case EFileDownloaderState.Paused: // * -> paused
                        FileDownloader?.OnFileDownloadPaused(new(remoteFilePath));
                        break;
                    case EFileDownloaderState.Downloading: // idle/resuming -> downloading
                        if (oldState is not EFileDownloaderState.Idle and not EFileDownloaderState.Resuming)
                        {
                            FileDownloader?.OnLogEmitted(new(level: ELogLevel.Warning, message: $"[FD.SCA.010] State changed to '{EFileDownloaderState.Downloading}' from an unexpected state '{oldState}' - this looks fishy so report this incident!", category: "FileDownloader", resource: remoteFilePath));
                        }

                        if (oldState == EFileDownloaderState.Resuming)
                        {
                            FileDownloader?.OnFileDownloadResumed(new(remoteFilePath)); //30
                        }
                        else // != resuming means we it just started
                        {
                            FileDownloader?.OnFileDownloadStarted(new(remoteFilePath, totalBytesToBeDownloaded)); //30
                        }

                        break;
                    case EFileDownloaderState.Complete: // idle/downloading/paused/resuming -> complete
                        if (oldState != EFileDownloaderState.Downloading) //00
                        {
                            FileDownloader?.OnLogEmitted(new(level: ELogLevel.Warning, message: $"[FD.SCA.050] State changed to 'complete' from an unexpected state '{oldState}' - this looks fishy so report this incident!", category: "FileDownloader", resource: remoteFilePath));
                        }

                        if (oldState is EFileDownloaderState.Paused or EFileDownloaderState.Resuming) // resuming/paused -> complete   (very rare cornercase)
                        {
                            FileDownloader?.OnFileDownloadResumed(new(remoteFilePath)); //workaround
                        }

                        FileDownloader?.OnFileDownloadProgressPercentageAndDataThroughputChanged(new(remoteFilePath: remoteFilePath, progressPercentage: 100, currentThroughputInKBps: 0, totalAverageThroughputInKBps: 0)); //50
                        FileDownloader?.OnFileDownloadCompleted(new(remoteFilePath: remoteFilePath, data: completeDownloadedData));
                        break;

                    //default: break; // idle error paused resuming cancelled cancelling    these have their own dedicated advertisements so we ignore them here    
                }

                //30   we took a conscious decision to have separate events for download-started/paused/resumed/completed and not to try to cram everything into
                //     the state-changed event   this is out of respect for the notion of separation-of-concerns
                //
                //50   trivial hotfix to deal with the fact that the file-download progress% doesnt fill up to 100%
                //
                //90   note that tiny files which are only a few bytes long can go idle->complete in a heartbeat skipping the 'downloading' state altogether
            }

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FileDownloader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(string resourceId, string errorMessage, EGlobalErrorCode globalErrorCode)
                => FileDownloader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(resourceId, errorMessage, globalErrorCode));

            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => FileDownloader?.OnFileDownloadProgressPercentageAndDataThroughputChanged(new FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(
                    remoteFilePath: resourceId,
                    progressPercentage: progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                ));
        }
    }
}