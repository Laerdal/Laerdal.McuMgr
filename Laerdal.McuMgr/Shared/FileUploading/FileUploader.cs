// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.AsyncX;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploading.Contracts.Native;

namespace Laerdal.McuMgr.FileUploading
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader, IFileUploaderEventEmittable
    {
        protected bool IsOperationOngoing;
        protected bool IsCancellationRequested;
        protected string CancellationReason = "";
        protected readonly object OperationCheckLock = new();
        protected readonly INativeFileUploaderProxy NativeFileUploaderProxy;

        public string LastFatalErrorMessage => NativeFileUploaderProxy?.LastFatalErrorMessage;
        
        protected readonly AsyncManualResetEvent KeepGoing = new(set: true); //related to pausing/unpausing   keepgoing=true by default

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileUploaderProxy
        internal FileUploader(INativeFileUploaderProxy nativeFileUploaderProxy)
        {
            NativeFileUploaderProxy = nativeFileUploaderProxy ?? throw new ArgumentNullException(nameof(nativeFileUploaderProxy));
            NativeFileUploaderProxy.FileUploader = this; //vital
        }
        
        protected bool IsDisposed;
        public void Dispose()
        {
            Dispose(isDisposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            if (!isDisposing)
                return;

            KeepGoing.Set(); // unblock any pause to let it observe the disposal
            
            try
            {
                NativeFileUploaderProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            IsDisposed = true;
        }
        
        public bool TrySetContext(object context) => NativeFileUploaderProxy?.TrySetContext(context) ?? false;
        public bool TrySetBluetoothDevice(object bluetoothDevice) => NativeFileUploaderProxy?.TrySetBluetoothDevice(bluetoothDevice) ?? false;
        public bool TryInvalidateCachedInfrastructure() => NativeFileUploaderProxy?.TryInvalidateCachedInfrastructure() ?? false;

        public EFileUploaderVerdict BeginUpload(
            byte[] data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize = null,
            int? pipelineDepth = null, //  ios
            int? byteAlignment = null, //  ios
            int? windowCapacity = null, // android
            int? memoryAlignment = null // android
        )
        {
            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));
            
            data = data ?? throw new ArgumentNullException(nameof(data));
            remoteFilePath = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePath(remoteFilePath);

            var failsafeConnectionSettings = ConnectionSettingsHelpers.GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,

                initialMtuSize: initialMtuSize,
                uploadingNotDownloading: true,

                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,

                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );
            if (failsafeConnectionSettings != null)
            {
                initialMtuSize = failsafeConnectionSettings.Value.initialMtuSize;
                pipelineDepth = failsafeConnectionSettings.Value.pipelineDepth;
                byteAlignment = failsafeConnectionSettings.Value.byteAlignment;
                windowCapacity = failsafeConnectionSettings.Value.windowCapacity;
                memoryAlignment = failsafeConnectionSettings.Value.memoryAlignment;
                
                OnLogEmitted(new LogEmittedEventArgs(
                    level: ELogLevel.Warning,
                    message: $"[FU.BU.010] Host device '{hostDeviceModel} (made by {hostDeviceManufacturer})' is known to be problematic. Resorting to using failsafe settings " +
                             $"(pipelineDepth={pipelineDepth ?.ToString() ?? "null"}, byteAlignment={byteAlignment?.ToString() ?? "null"}, initialMtuSize={initialMtuSize?.ToString() ?? "null"}, windowCapacity={windowCapacity?.ToString() ?? "null"}, memoryAlignment={memoryAlignment?.ToString() ?? "null"})",
                    resource: resourceId,
                    category: "FileDownloader"
                ));
            }

            var verdict = NativeFileUploaderProxy.BeginUpload(
                data: data,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,

                initialMtuSize: initialMtuSize,
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );

            return verdict;
        }

        public bool TryPause()
        {
            if (IsDisposed || IsCancellationRequested || !IsOperationOngoing)
                return false;

            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Trace, message: "[FU.TPS.010] Received request to pause the ongoing upload operation", category: "FileUploader", resource: ""));

            KeepGoing.Reset(); //order                     blocks any ongoing installation/verification
            NativeFileUploaderProxy?.TryPause(); //order   ignore the return value

            return true; //must always return true
        }

        public bool TryResume()
        {
            if (IsDisposed || IsCancellationRequested || !IsOperationOngoing)
                return false;

            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Trace, message: "[FU.TRS.010] Received request to resume the upload operation (if any)", category: "FileUploader", resource: ""));

            KeepGoing.Set(); //order                         unblocks any ongoing installation/verification
            NativeFileUploaderProxy?.TryResume(); //order    ignore the return value

            return true; //must always return true
        }

        public bool TryCancel(string reason = "")
        {
            IsCancellationRequested = true; //order

            var success = NativeFileUploaderProxy?.TryCancel(reason) ?? false; //order

            KeepGoing.Set(); //order   unblocks any ongoing installation/verification so that it can observe the cancellation

            return success;
        }

        public bool TryDisconnect() => NativeFileUploaderProxy?.TryDisconnect() ?? false;
        public void CleanupResourcesOfLastUpload() => NativeFileUploaderProxy?.CleanupResourcesOfLastUpload();
        
        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<CancellingEventArgs> _cancelling;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FileUploadPausedEventArgs> _fileUploadPaused;
        private event EventHandler<FileUploadStartedEventArgs> _fileUploadStarted;
        private event EventHandler<FileUploadResumedEventArgs> _fileUploadResumed;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event EventHandler<FileUploadCompletedEventArgs> _fileUploadCompleted;
        private event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> _fileUploadProgressPercentageAndDataThroughputChanged;

        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred
        {
            add
            {
                _fatalErrorOccurred -= value;
                _fatalErrorOccurred += value;
            }
            remove => _fatalErrorOccurred -= value;
        }

        public event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted
        {
            add
            {
                _logEmitted -= value;
                _logEmitted += value;
            }
            remove => _logEmitted -= value;
        }

        public event EventHandler<CancellingEventArgs> Cancelling
        {
            add
            {
                _cancelling -= value;
                _cancelling += value;
            }
            remove => _cancelling -= value;
        }

        public event EventHandler<CancelledEventArgs> Cancelled
        {
            add
            {
                _cancelled -= value;
                _cancelled += value;
            }
            remove => _cancelled -= value;
        }

        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged
        {
            add
            {
                _busyStateChanged -= value;
                _busyStateChanged += value;
            }
            remove => _busyStateChanged -= value;
        }
        
        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            add
            {
                _stateChanged -= value;
                _stateChanged += value;
            }
            remove => _stateChanged -= value;
        }
        
        public event EventHandler<FileUploadStartedEventArgs> FileUploadStarted
        {
            add
            {
                _fileUploadStarted -= value;
                _fileUploadStarted += value;
            }
            remove => _fileUploadStarted -= value;
        }

        public event EventHandler<FileUploadPausedEventArgs> FileUploadPaused
        {
            add
            {
                _fileUploadPaused -= value;
                _fileUploadPaused += value;
            }
            remove => _fileUploadPaused -= value;
        }
        
        public event EventHandler<FileUploadResumedEventArgs> FileUploadResumed
        {
            add
            {
                _fileUploadResumed -= value;
                _fileUploadResumed += value;
            }
            remove => _fileUploadResumed -= value;
        }
        
        public event EventHandler<FileUploadCompletedEventArgs> FileUploadCompleted
        {
            add
            {
                _fileUploadCompleted -= value;
                _fileUploadCompleted += value;
            }
            remove => _fileUploadCompleted -= value;
        }

        public event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> FileUploadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _fileUploadProgressPercentageAndDataThroughputChanged -= value;
                _fileUploadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _fileUploadProgressPercentageAndDataThroughputChanged -= value;
        }

        public async Task<IEnumerable<string>> UploadAsync<TData>(
            IDictionary<string, (string ResourceId, TData Data)> remoteFilePathsAndTheirData,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int sleepTimeBetweenUploadsInMs = 0,
            int sleepTimeBetweenRetriesInMs = 100,
            int timeoutPerUploadInMs = -1,
            int maxTriesPerUpload = 10,
            bool moveToNextUploadInCaseOfError = true,
            bool autodisposeStreams = false,
            int? initialMtuSize = null,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        ) where TData : notnull
        {
            EnsureExclusiveOperation(); //keep this outside of the try-finally block!
            ResetInternalStateTidbits();
            
            try
            {
                return await UploadCoreAsync_();
            }
            finally
            {
                ReleaseExclusiveOperation();
            }

            async Task<IEnumerable<string>> UploadCoreAsync_()
            {
                if (string.IsNullOrWhiteSpace(hostDeviceModel))
                    throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

                if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                    throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));
            
                if (sleepTimeBetweenUploadsInMs < 0)
                    throw new ArgumentOutOfRangeException(nameof(sleepTimeBetweenUploadsInMs), sleepTimeBetweenUploadsInMs, "Must be greater than or equal to zero");

                var sanitizedRemoteFilePathsAndTheirData = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePathsWithData(remoteFilePathsAndTheirData);

                var lastIndex = sanitizedRemoteFilePathsAndTheirData.Count - 1;
                var filesThatFailedToBeUploaded = (List<string>) null;
                foreach (var ((remoteFilePath, (resourceId, data)), i) in sanitizedRemoteFilePathsAndTheirData.Select((x, i) => (x, i)))
                {
                    try
                    {
                        await SingleUploadImplAsync(
                            data: data,
                            resourceId: resourceId,
                            remoteFilePath: remoteFilePath,

                            hostDeviceModel: hostDeviceModel,
                            hostDeviceManufacturer: hostDeviceManufacturer,

                            maxTriesCount: maxTriesPerUpload,
                            timeoutForUploadInMs: timeoutPerUploadInMs,
                            sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,

                            autodisposeStream: autodisposeStreams,
                        
                            initialMtuSize: initialMtuSize, //    both ios and android
                            pipelineDepth: pipelineDepth, //      ios only
                            byteAlignment: byteAlignment, //      ios only
                            windowCapacity: windowCapacity, //    android only
                            memoryAlignment: memoryAlignment //   android only
                        );

                        if (sleepTimeBetweenUploadsInMs > 0 && i < lastIndex) //we skip sleeping after the last upload
                        {
                            await Task.Delay(sleepTimeBetweenUploadsInMs);
                        }
                    }
                    catch (UploadErroredOutException)
                    {
                        if (moveToNextUploadInCaseOfError) //00
                        {
                            (filesThatFailedToBeUploaded ??= new List<string>(4)).Add(remoteFilePath);
                            continue;
                        }

                        throw;
                    }
                }

                return filesThatFailedToBeUploaded ?? Enumerable.Empty<string>();

                //00  we prefer to upload as many files as possible and report any failures collectively at the very end   we resorted to this
                //    tactic because failures are fairly common when uploading 50 files or more over to mcumgr devices, and we wanted to ensure
                //    that it would be as easy as possible to achieve the mass uploading just by using the default settings
            }
        }
        
        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;

        public async Task UploadAsync<TData>(
            TData data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutForUploadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500,
            bool autodisposeStream = false,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        ) where TData : notnull
        {
            EnsureExclusiveOperation(); //keep this outside of the try-finally block!
            ResetInternalStateTidbits();

            try
            {
                await SingleUploadImplAsync(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,
                    
                    maxTriesCount: maxTriesCount,
                    timeoutForUploadInMs: timeoutForUploadInMs,
                    
                    sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,
                    gracefulCancellationTimeoutInMs: gracefulCancellationTimeoutInMs,
                    autodisposeStream: autodisposeStream,
                    
                    initialMtuSize: initialMtuSize, //    both ios and android
                    pipelineDepth: pipelineDepth, //      ios only
                    byteAlignment: byteAlignment, //      ios only
                    windowCapacity: windowCapacity, //    android only
                    memoryAlignment: memoryAlignment //   android only
                );
            }
            finally
            {
                ReleaseExclusiveOperation();
            }
        }

        private async Task SingleUploadImplAsync<TData>(
            TData data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutForUploadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500,
            bool autodisposeStream = false,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        ) where TData : notnull
        {
            //EnsureExclusiveOperation_(); //dont   this should never be checked here
            
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
                    await CheckIfPausedAsync(resourceId: resourceId, remoteFilePath: remoteFilePath); //order

                    if (IsCancellationRequested) //order   keep inside the try-catch block
                        throw new UploadCancelledException(CancellationReason);

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

                    var verdict = BeginUpload( //00 dont use task.run here for now
                        data: dataArray,
                        resourceId: resourceId,
                        remoteFilePath: remoteFilePath,
                        hostDeviceModel: hostDeviceModel,
                        hostDeviceManufacturer: hostDeviceManufacturer,

                        initialMtuSize: initialMtuSize,

                        pipelineDepth: pipelineDepth, //      ios only
                        windowCapacity: windowCapacity, //    ios only
                        byteAlignment: byteAlignment, //      android only
                        memoryAlignment: memoryAlignment //   android only
                    );
                    if (verdict != EFileUploaderVerdict.Success)
                        throw verdict == EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress
                            ? new InvalidOperationException("Another upload operation is already in progress") //impossible at this point but just in case
                            : new ArgumentException(verdict.ToString());

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

                    throw new UploadTimeoutException(remoteFilePath, timeoutForUploadInMs, ex);
                }
                catch (UploadErroredOutException ex) //errors with code in_value(3) and even UnauthorizedException happen all the time in android when multiuploading files
                {
                    if (ex is UploadErroredOutRemoteFolderNotFoundException) //order    no point to retry if any of the remote parent folders are not there
                        throw;

                    if (++triesCount > maxTriesCount) //order
                        throw new AllUploadAttemptsFailedException(remoteFilePath, maxTriesCount, innerException: ex);

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
                    
                    throw new UploadInternalErrorException(remoteFilePath, ex);
                }
                finally
                {
                    Cancelled -= FileUploader_Cancelled_;
                    Cancelling -= FileUploader_Cancelling_;
                    StateChanged -= FileUploader_StateChanged_;
                    FatalErrorOccurred -= FileUploader_FatalErrorOccurred_;
                    FileUploadCompleted -= FileUploader_FileUploadCompleted_;
                    FileUploadProgressPercentageAndDataThroughputChanged -= FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_;
                    
                    CleanupResourcesOfLastUpload(); //vital
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
                        EGlobalErrorCode.SubSystemFilesystem_NotFound => new UploadErroredOutRemoteFolderNotFoundException(remoteFilePath: remoteFilePath, nativeErrorMessage: ea_.ErrorMessage, globalErrorCode: ea_.GlobalErrorCode),
                        EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied => new UploadUnauthorizedException(remoteFilePath: remoteFilePath, nativeErrorMessage: ea_.ErrorMessage, globalErrorCode: ea_.GlobalErrorCode),
                        _ => new UploadErroredOutException(remoteFilePath: remoteFilePath, globalErrorCode: ea_.GlobalErrorCode, nativeErrorMessage: ea_.ErrorMessage)
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

        void ILogEmittable.OnLogEmitted(in LogEmittedEventArgs ea) => OnLogEmitted(in ea);
        void IFileUploaderEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea);
        void IFileUploaderEventEmittable.OnCancelling(CancellingEventArgs ea) => OnCancelling(ea);
        void IFileUploaderEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFileUploaderEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFileUploaderEventEmittable.OnFileUploadPaused(FileUploadPausedEventArgs ea) => OnFileUploadPaused(ea);
        void IFileUploaderEventEmittable.OnFileUploadStarted(FileUploadStartedEventArgs ea) => OnFileUploadStarted(ea);
        void IFileUploaderEventEmittable.OnFileUploadResumed(FileUploadResumedEventArgs ea) => OnFileUploadResumed(ea);
        void IFileUploaderEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFileUploaderEventEmittable.OnFileUploadCompleted(FileUploadCompletedEventArgs ea) => OnFileUploadCompleted(ea);
        void IFileUploaderEventEmittable.OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFileUploadProgressPercentageAndDataThroughputChanged(ea);

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnCancelling(CancellingEventArgs ea) => _cancelling?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnLogEmitted(in LogEmittedEventArgs ea) => _logEmitted?.InvokeAndIgnoreExceptions(this, ea); // in the special case of log-emitted we prefer the .invoke() flavour for the sake of performance
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);

        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileUploadProgressPercentageAndDataThroughputChanged?.InvokeAndIgnoreExceptions(this, ea);

        private void OnFileUploadCompleted(FileUploadCompletedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Trace, message: "[FU.OFUC.010] Uploading complete", category: "FileUploader", resource: ea.ResourceId));
            
            _fileUploadCompleted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }

        private void OnFileUploadPaused(FileUploadPausedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FU.OFUP.010] Uploading paused", category: "FileUploader", resource: ea.ResourceId));

            _fileUploadPaused?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileUploadStarted(FileUploadStartedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: $"[FU.OFUS.010] Starting uploading of '{ea.TotalBytesToBeUploaded}' bytes", category: "FileUploader", resource: ea.ResourceId));

            _fileUploadStarted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFileUploadResumed(FileUploadResumedEventArgs ea)
        {
            OnLogEmitted(new(level: ELogLevel.Info, message: "[FU.OFUR.010] Resumed uploading of asset", category: "FileUploader", resource: ea.ResourceId));

            _fileUploadResumed?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }
        
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea)
        {
            OnLogEmitted(new LogEmittedEventArgs(level: ELogLevel.Error, message: $"[{nameof(ea.GlobalErrorCode)}='{ea.GlobalErrorCode}'] {ea.ErrorMessage}", resource: ea.RemoteFilePath, category: "file-uploader"));
            
            _fatalErrorOccurred?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }

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
        
        protected void EnsureExclusiveOperation()
        {
            lock (OperationCheckLock)
            {
                if (IsOperationOngoing)
                    throw new InvalidOperationException("An upload operation is already running - cannot start another one");
                
                IsOperationOngoing = true;    
            }
        }

        protected void ReleaseExclusiveOperation()
        {
            lock (OperationCheckLock)
            {
                IsOperationOngoing = false;
            }
        }
        
        protected virtual void ResetInternalStateTidbits()
        {
            //IsOperationOngoing = false; //dont

            CancellationReason = "";
            IsCancellationRequested = false;

            KeepGoing.Set(); // unblocks any ongoing installation/verification    just in case
        }
        
        protected virtual async Task CheckIfPausedAsync(string resourceId, string remoteFilePath)
        {
            var mustPauseHere = !KeepGoing.IsSet; //00
            if (mustPauseHere)
            {
                OnStateChanged(new(resourceId: resourceId, remoteFilePath: remoteFilePath, EFileUploaderState.None, EFileUploaderState.Paused)); //  we kinda emulate the behavior 
                OnFileUploadPaused(new(resourceId: resourceId, remoteFilePath: remoteFilePath)); //                                                  of the native layer
            }
        
            await KeepGoing.WaitAsync(); //10
            
            if (mustPauseHere)
            {
                OnStateChanged(new(resourceId: resourceId, remoteFilePath: remoteFilePath, EFileUploaderState.Paused, EFileUploaderState.None)); //  we kinda emulate the behavior of the native layer
                OnFileUploadResumed(new(resourceId: resourceId, remoteFilePath: remoteFilePath)); //00                                               (we skip the 'resuming' state though)
            }
        
            //00  if the semaphore count is zero it means we are about to get paused without any ongoing
            //    transfers so we will emit the paused/resumed events here to keep things consistent
            //
            //10  we just want to check if we are paused/cancelled   this will block if we are paused
            //    immediately release again   we dont want to postpone this any longer than necessary
        }
    }
}
