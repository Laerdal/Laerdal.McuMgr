using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;

        public async Task<byte[]> DownloadAsync(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutForDownloadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = DefaultGracefulCancellationTimeoutInMs,
            int? initialMtuSize = null,
            int? windowCapacity = null
        )
        {
            EnsureExclusiveOperationToken(); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                return await SingleDownloadCoreAsync(
                    remoteFilePath: remoteFilePath,

                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,

                    maxTriesCount: maxTriesCount,
                    timeoutForDownloadInMs: timeoutForDownloadInMs,
                    sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,
                    gracefulCancellationTimeoutInMs: gracefulCancellationTimeoutInMs,

                    initialMtuSize: initialMtuSize,
                    windowCapacity: windowCapacity
                );
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }
        }
        
        protected async Task<byte[]> SingleDownloadCoreAsync(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutForDownloadInMs,
            int maxTriesCount,
            int sleepTimeBetweenRetriesInMs,
            int gracefulCancellationTimeoutInMs,
            int? initialMtuSize,
            int? windowCapacity
        )
        {
            if (maxTriesCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTriesCount), maxTriesCount, "Must be greater than zero!");

            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

            gracefulCancellationTimeoutInMs = gracefulCancellationTimeoutInMs >= 0 //we want to ensure that the timeout is always sane
                ? gracefulCancellationTimeoutInMs
                : DefaultGracefulCancellationTimeoutInMs;

            var result = (byte[]) null;
            var cancellationReason = "";
            var isCancellationRequested = false;
            var fileDownloadProgressEventsCount = 0;
            var suspiciousTransportFailuresCount = 0;
            var didWarnOnceAboutUnstableConnection = false;
            for (var triesCount = 1; !isCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSourceRCA<byte[]>(state: null);

                try
                {
                    Cancelled += FileDownloader_Cancelled_;
                    Cancelling += FileDownloader_Cancelling_;
                    StateChanged += FileDownloader_StateChanged_;
                    FatalErrorOccurred += FileDownloader_FatalErrorOccurred_;
                    FileDownloadCompleted += FileDownloader_FileDownloadCompleted_;
                    FileDownloadProgressPercentageAndDataThroughputChanged += FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_;

                    var failSafeSettingsToApply = ConnectionSettingsHelpers.GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable(
                        uploadingNotDownloading: false,
                        triesCount: triesCount,
                        maxTriesCount: maxTriesCount,
                        suspiciousTransportFailuresCount: suspiciousTransportFailuresCount
                    );
                    if (failSafeSettingsToApply != null)
                    {
                        initialMtuSize = failSafeSettingsToApply.Value.initialMtuSize;
                        windowCapacity = failSafeSettingsToApply.Value.windowCapacity;

                        if (!didWarnOnceAboutUnstableConnection)
                        {
                            didWarnOnceAboutUnstableConnection = true;
                            OnLogEmitted(new LogEmittedEventArgs(
                                level: ELogLevel.Warning,
                                message: $"[FD.DA.010] Attempt#{triesCount}: Connection is too unstable for downloading assets from the target device. Subsequent tries will use failsafe parameters " +
                                         $"on the connection just in case it helps (initialMtuSize={initialMtuSize?.ToString() ?? "null"}, windowCapacity={windowCapacity?.ToString() ?? "null"})",
                                resource: "File",
                                category: "FileDownloader"
                            ));
                        }
                    }

                    var verdict = BeginDownloadCore( //00 dont use task.run here for now
                        remoteFilePath: remoteFilePath,
                        hostDeviceModel: hostDeviceModel,
                        hostDeviceManufacturer: hostDeviceManufacturer,

                        initialMtuSize: initialMtuSize,
                        windowCapacity: windowCapacity
                    );
                    if (verdict != EFileDownloaderVerdict.Success)
                        throw verdict == EFileDownloaderVerdict.FailedDownloadAlreadyInProgress
                            ? new InvalidOperationException("Another download operation is already in progress")
                            : new ArgumentException(verdict.ToString());

                    result = await taskCompletionSource.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeoutForDownloadInMs);
                    break;
                }
                catch (TimeoutException ex)
                {
                    //todo   silently cancel the download here on best effort basis

                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        remoteFilePath: remoteFilePath,
                        oldState: EFileDownloaderState.None, //better not use this.State here because the native call might fail
                        newState: EFileDownloaderState.Error
                    ));

                    throw new DownloadTimeoutException(remoteFilePath, timeoutForDownloadInMs, ex);
                }
                catch (DownloadErroredOutException ex)
                {
                    if (ex is DownloadErroredOutRemoteFileNotFoundException or DownloadErroredOutRemotePathPointsToDirectoryException) //order   no point to retry if the filepath is problematic
                    {
                        //OnStateChanged(new StateChangedEventArgs(newState: EFileDownloaderState.Error)); //noneed   already done in native code
                        throw;
                    }

                    if (++triesCount > maxTriesCount) //order
                    {
                        //OnStateChanged(new StateChangedEventArgs(newState: EFileDownloaderState.Error)); //noneed   already done in native code
                        throw new AllDownloadAttemptsFailedException(remoteFilePath, maxTriesCount, innerException: ex);
                    }

                    if (fileDownloadProgressEventsCount <= 10)
                    {
                        suspiciousTransportFailuresCount++;
                    }

                    if (sleepTimeBetweenRetriesInMs > 0) //order
                    {
                        await Task.Delay(sleepTimeBetweenRetriesInMs);
                    }

                    continue;
                }
                catch (Exception ex) when (
                    ex is not ArgumentException //10 wops probably missing native lib symbols!
                    && ex is not TimeoutException
                    && !(ex is IDownloadException) //this accounts for both cancellations and download exceptions!
                )
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        remoteFilePath: remoteFilePath,
                        oldState: EFileDownloaderState.None,
                        newState: EFileDownloaderState.Error
                    ));

                    //OnFatalErrorOccurred(); //dont   not worth it in this case  

                    throw new DownloadInternalErrorException(ex);
                }
                finally
                {
                    taskCompletionSource.TrySetCanceled(); //it is best to ensure that the task is fossilized in case of rogue exceptions

                    Cancelled -= FileDownloader_Cancelled_;
                    Cancelling -= FileDownloader_Cancelling_;
                    StateChanged -= FileDownloader_StateChanged_;
                    FatalErrorOccurred -= FileDownloader_FatalErrorOccurred_;
                    FileDownloadCompleted -= FileDownloader_FileDownloadCompleted_;
                    FileDownloadProgressPercentageAndDataThroughputChanged -= FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_;
                }

                void FileDownloader_Cancelled_(object sender_, CancelledEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(new DownloadCancelledException(ea_.Reason));
                }

                void FileDownloader_Cancelling_(object _, CancellingEventArgs ea_)
                {
                    if (isCancellationRequested)
                        return;

                    cancellationReason = ea_.Reason;
                    isCancellationRequested = true;

                    Task.Run(async () =>
                    {
                        try
                        {
                            if (gracefulCancellationTimeoutInMs > 0) //keep this check here to avoid unnecessary task rescheduling
                            {
                                await Task.Delay(gracefulCancellationTimeoutInMs);
                            }

                            OnCancelled(new CancelledEventArgs(ea_.Reason)); //00
                        }
                        catch // (Exception ex)
                        {
                            // ignored
                        }
                    });

                    //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see OnCancelled()
                    //    getting called right above   but if that takes too long we give the killing blow by calling OnCancelled() manually here
                }

                void FileDownloader_StateChanged_(object sender_, StateChangedEventArgs ea_)
                {
                    switch (ea_.NewState)
                    {
                        case EFileDownloaderState.Idle:
                            fileDownloadProgressEventsCount = 0;
                            return;
                    }

                    //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see OnCancelled()
                    //    getting called right above   but if that takes too long we give the killing blow by calling OnCancelled() manually here
                }

                void FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_(object _, FileDownloadProgressPercentageAndDataThroughputChangedEventArgs __)
                {
                    fileDownloadProgressEventsCount++;
                }

                void FileDownloader_FileDownloadCompleted_(object _, FileDownloadCompletedEventArgs ea_)
                {
                    taskCompletionSource.TrySetResult(ea_.Data);
                }

                void FileDownloader_FatalErrorOccurred_(object _, FatalErrorOccurredEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(ea_.GlobalErrorCode switch
                    {
                        EGlobalErrorCode.SubSystemFilesystem_NotFound => new DownloadErroredOutRemoteFileNotFoundException(remoteFilePath), // remote file not found
                        EGlobalErrorCode.SubSystemFilesystem_IsDirectory => new DownloadErroredOutRemotePathPointsToDirectoryException(remoteFilePath), // remote filepath points to a directory
                        EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied => new UnauthorizedException(remoteFilePath, ea_.ErrorMessage), // unauthorized
                        _ => new DownloadErroredOutException(remoteFilePath, ea_.GlobalErrorCode)
                    });
                }
            }

            if (isCancellationRequested) //vital
                throw new DownloadCancelledException(cancellationReason); //20

            return result;

            //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
            //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
            //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
            //
            //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
            //
            //20  its important to detect the cancellation request so as to break as early as possible    this becomes even more important
            //    in cases where the ble connection bites the dust and is unrecoverable because in that case the file downloader will just keep
            //    on trying in vain forever for like 50 retries or something and pressing the cancel button wont have any effect because
            //    the download cannot commence to begin with
        }
    }
}