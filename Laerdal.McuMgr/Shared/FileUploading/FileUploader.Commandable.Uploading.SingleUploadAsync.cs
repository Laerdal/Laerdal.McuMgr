// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;

        public async Task UploadAsync<TData>( //@formatter:off
            TData data,

            string resourceId,
            string remoteFilePath,
            
            string hostDeviceModel,
            string hostDeviceManufacturer,
            
            int        timeoutForUploadInMs            = IFileUploaderCommandable.Defaults.TimeoutPerUploadInMs,
            int        maxTriesCount                   = IFileUploaderCommandable.Defaults.MaxTriesPerUpload,
            int        sleepTimeBetweenRetriesInMs     = IFileUploaderCommandable.Defaults.SleepTimeBetweenRetriesInMs,
            int        gracefulCancellationTimeoutInMs = IFileUploaderCommandable.Defaults.GracefulCancellationTimeoutInMs,
            bool       autodisposeStream               = IFileUploaderCommandable.Defaults.AutodisposeStreams,
            
            ELogLevel? minimumNativeLogLevel           = null,
            
            int? pipelineDepth   = null,
            int? byteAlignment   = null,
            int? initialMtuSize  = null,
            int? windowCapacity  = null,
            int? memoryAlignment = null
        ) where TData : notnull //@formatter:on
        {
            await EnsureExclusiveOperationTokenAsync().ConfigureAwait(false); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                await SingleUploadCoreAsync(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,

                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,

                    maxTriesCount: maxTriesCount,
                    timeoutForUploadInMs: timeoutForUploadInMs,

                    minimumNativeLogLevel: minimumNativeLogLevel,
                    autodisposeStream: autodisposeStream,
                    
                    sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,
                    gracefulCancellationTimeoutInMs: gracefulCancellationTimeoutInMs,

                    initialMtuSize: initialMtuSize, //    both ios and android
                    pipelineDepth: pipelineDepth, //      ios only
                    byteAlignment: byteAlignment, //      ios only
                    windowCapacity: windowCapacity, //    android only
                    memoryAlignment: memoryAlignment //   android only
                );
            }
            finally
            {
                await ReleaseExclusiveOperationTokenAsync().ConfigureAwait(false);
            }
        }
        
        protected async Task SingleUploadCoreAsync<TData>(
            TData data,
            string resourceId,
            string remoteFilePath,

            string hostDeviceModel,
            string hostDeviceManufacturer,

            int timeoutForUploadInMs, //            = Defaults.TimeoutPerUploadInMs
            int maxTriesCount, //                   = Defaults.MaxTriesPerUpload
            int sleepTimeBetweenRetriesInMs, //     = Defaults.SleepTimeBetweenRetriesInMs
            int gracefulCancellationTimeoutInMs, // = Defaults.GracefulCancellationTimeoutInMs
            bool autodisposeStream, //              = Defaults.AutodisposeStreams
            ELogLevel? minimumNativeLogLevel, //          = null

            int? pipelineDepth, //                  = null,
            int? byteAlignment, //                  = null,
            int? initialMtuSize, //                 = null,
            int? windowCapacity, //                 = null,
            int? memoryAlignment //                 = null
        ) where TData : notnull
        {
            //EnsureExclusiveOperationToken_(); //dont   this should never be checked here

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (maxTriesCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTriesCount), maxTriesCount, "Must be greater than zero");

            if (sleepTimeBetweenRetriesInMs < 0)
                throw new ArgumentOutOfRangeException(nameof(sleepTimeBetweenRetriesInMs), sleepTimeBetweenRetriesInMs, "Must be greater than or equal to zero");

            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

            var dataArray = await GetDataAsByteArray_(data, autodisposeStream);

            gracefulCancellationTimeoutInMs = gracefulCancellationTimeoutInMs >= 0 //we want to ensure that the timeout is always sane
                ? gracefulCancellationTimeoutInMs
                : DefaultGracefulCancellationTimeoutInMs;

            var fileUploadProgressEventsCount = 0;
            var suspiciousTransportFailuresCount = 0;
            var didWarnOnceAboutUnstableConnection = false;
            var isDeathknellCancellationTaskBeenLaunched = false;
            for (var triesCount = 1; !IsCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSourceRCA<bool>(state: false);
                try
                {
                    await CheckIfPausedOrCancelledAsync(resourceId: resourceId, remoteFilePath: remoteFilePath); //order

                    Cancelled += FileUploader_Cancelled_;
                    Cancelling += FileUploader_Cancelling_;
                    StateChanged += FileUploader_StateChanged_;
                    FatalErrorOccurred += FileUploader_FatalErrorOccurred_;
                    FileUploadCompleted += FileUploader_FileUploadCompleted_;
                    FileUploadProgressPercentageAndDataThroughputChanged += FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_;

                    var failSafeSettingsToApply = ConnectionSettingsHelpers.GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable(
                        uploadingNotDownloading: true,
                        triesCount: triesCount,
                        maxTriesCount: maxTriesCount,
                        suspiciousTransportFailuresCount: suspiciousTransportFailuresCount
                    );
                    if (failSafeSettingsToApply != null)
                    {
                        byteAlignment = failSafeSettingsToApply.Value.byteAlignment;
                        pipelineDepth = failSafeSettingsToApply.Value.pipelineDepth;
                        initialMtuSize = failSafeSettingsToApply.Value.initialMtuSize;
                        windowCapacity = failSafeSettingsToApply.Value.windowCapacity;
                        memoryAlignment = failSafeSettingsToApply.Value.memoryAlignment;

                        if (!didWarnOnceAboutUnstableConnection)
                        {
                            didWarnOnceAboutUnstableConnection = true;
                            OnLogEmitted(new LogEmittedEventArgs(
                                level: ELogLevel.Warning,
                                message: $"[FU.UA.010] Attempt#{triesCount}: Connection is too unstable for uploading assets to the target device. Subsequent tries will use failsafe parameters on the connection " +
                                         $"just in case it helps (byteAlignment={byteAlignment}, pipelineDepth={pipelineDepth}, initialMtuSize={initialMtuSize}, windowCapacity={windowCapacity}, memoryAlignment={memoryAlignment})",
                                resource: resourceId,
                                category: "FileUploader"
                            ));
                        }
                    }

                    BeginUploadCore( //00 dont use task.run here for now
                        data: dataArray,
                        resourceId: resourceId,
                        remoteFilePath: remoteFilePath,
                        hostDeviceModel: hostDeviceModel,
                        hostDeviceManufacturer: hostDeviceManufacturer,

                        initialMtuSize: initialMtuSize,
                        minimumNativeLogLevel: minimumNativeLogLevel,

                        pipelineDepth: pipelineDepth, //      ios only
                        windowCapacity: windowCapacity, //    ios only
                        byteAlignment: byteAlignment, //      android only
                        memoryAlignment: memoryAlignment //   android only
                    );

                    await taskCompletionSource.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeoutForUploadInMs); //order
                    break;
                }
                catch (TimeoutException ex)
                {
                    //todo   silently cancel the upload here on best effort basis

                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: EFileUploaderState.None, //better not use this.State here because the native call might fail
                        newState: EFileUploaderState.Error,
                        resourceId: resourceId,
                        remoteFilePath: remoteFilePath
                    ));

                    throw new FileUploadTimeoutException(remoteFilePath, timeoutForUploadInMs, ex);
                }
                catch (FileUploadErroredOutException ex) //errors with code in_value(3) and even UnauthorizedException happen all the time in android when multiuploading files
                {
                    if (ex is FileUploadErroredOutRemoteFolderNotFoundException) //order    no point to retry if any of the remote parent folders are not there
                        throw;

                    if (++triesCount > maxTriesCount) //order
                        throw new AllFileUploadAttemptsFailedException(remoteFilePath, maxTriesCount, innerException: ex);

                    if (fileUploadProgressEventsCount <= 10)
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
                    && ex is not ObjectDisposedException
                    && !(ex is IUploadException) //this accounts for both cancellations and upload errors
                )
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: EFileUploaderState.None,
                        newState: EFileUploaderState.Error,
                        resourceId: resourceId,
                        remoteFilePath: remoteFilePath
                    ));

                    // OnFatalErrorOccurred(); //better not   too much fuss

                    throw new FileUploadInternalErrorException(remoteFilePath, innerException: ex);
                }
                finally
                {
                    Cancelled -= FileUploader_Cancelled_;
                    Cancelling -= FileUploader_Cancelling_;
                    StateChanged -= FileUploader_StateChanged_;
                    FatalErrorOccurred -= FileUploader_FatalErrorOccurred_;
                    FileUploadCompleted -= FileUploader_FileUploadCompleted_;
                    FileUploadProgressPercentageAndDataThroughputChanged -= FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_;

                    TryCleanupResourcesOfLastUpload(); //vital
                }

                void FileUploader_Cancelled_(object _, CancelledEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(new UploadCancelledException(ea_.Reason));
                }

                void FileUploader_FileUploadCompleted_(object _, FileUploadCompletedEventArgs ea_)
                {
                    taskCompletionSource.TrySetResult(true);
                }

                void FileUploader_Cancelling_(object __, CancellingEventArgs ea_)
                {
                    if (isDeathknellCancellationTaskBeenLaunched)
                        return;

                    CancellationReason = ea_.Reason;
                    IsCancellationRequested = true;
                    isDeathknellCancellationTaskBeenLaunched = true;

                    _ = Task.Run(async () =>
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

                void FileUploader_StateChanged_(object _, StateChangedEventArgs ea_) // ReSharper disable AccessToModifiedClosure
                {
                    switch (ea_.NewState)
                    {
                        case EFileUploaderState.Idle:
                            fileUploadProgressEventsCount = 0; //it is vital to reset the counter here to account for retries
                            return;

                        case EFileUploaderState.Complete:
                            //taskCompletionSource.TrySetResult(true); //dont   we want to wait for the FileUploadCompleted event
                            return;
                    }

                    //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see OnCancelled()
                    //    getting called right above   but if that takes too long we give the killing blow by calling OnCancelled() manually here
                } // ReSharper restore AccessToModifiedClosure

                void FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_(object _, FileUploadProgressPercentageAndDataThroughputChangedEventArgs __)
                {
                    fileUploadProgressEventsCount++;
                }

                void FileUploader_FatalErrorOccurred_(object _, FatalErrorOccurredEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(ea_.GlobalErrorCode switch
                    {
                        EGlobalErrorCode.SubSystemFilesystem_NotFound => new FileUploadErroredOutRemoteFolderNotFoundException(remoteFilePath: remoteFilePath, nativeErrorMessage: ea_.ErrorMessage, globalErrorCode: ea_.GlobalErrorCode),
                        EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied => new FileUploadUnauthorizedException(remoteFilePath: remoteFilePath, nativeErrorMessage: ea_.ErrorMessage, globalErrorCode: ea_.GlobalErrorCode),
                        _ => new FileUploadErroredOutException(remoteFilePath: remoteFilePath, globalErrorCode: ea_.GlobalErrorCode, nativeErrorMessage: ea_.ErrorMessage)
                    });
                }
            }

            if (IsCancellationRequested) //vital
                throw new UploadCancelledException(CancellationReason); //20

            return;

            //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
            //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
            //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
            //
            //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
            //
            //20  its important to detect the cancellation request so as to break as early as possible    this becomes even more important
            //    in cases where the ble connection bites the dust and is unrecoverable because in that case the file uploader will just keep
            //    on trying in vain forever for like 50 retries or something and pressing the cancel button wont have any effect because
            //    the upload cannot commence to begin with

            static async Task<byte[]> GetDataAsByteArray_<TD>(TD dataObject_, bool autodisposeStream_) => dataObject_ switch
            {
                Stream dataStream => await dataStream.ReadBytesAsync(disposeStream: autodisposeStream_),

                Func<Stream> openCallback => await openCallback().ReadBytesAsync(disposeStream: autodisposeStream_),
                Func<Task<Stream>> openAsyncCallback => await (await openAsyncCallback()).ReadBytesAsync(disposeStream: autodisposeStream_),
                Func<ValueTask<Stream>> openAsyncCallback => await (await openAsyncCallback()).ReadBytesAsync(disposeStream: autodisposeStream_),

                byte[] dataByteArray => dataByteArray,
                IEnumerable<byte> dataEnumerableBytes => dataEnumerableBytes.ToArray(), //just in case

                _ => throw new NotSupportedException($"Unsupported data type {dataObject_?.GetType().FullName ?? "N/A"} passed to UploadAsync()")
            };
        }
    }
}
