// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader, IFileUploaderEventEmittable
    {
        private readonly INativeFileUploaderProxy _nativeFileUploaderProxy;

        public string LastFatalErrorMessage => _nativeFileUploaderProxy?.LastFatalErrorMessage;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileUploaderProxy
        internal FileUploader(INativeFileUploaderProxy nativeFileUploaderProxy)
        {
            _nativeFileUploaderProxy = nativeFileUploaderProxy ?? throw new ArgumentNullException(nameof(nativeFileUploaderProxy));
            _nativeFileUploaderProxy.FileUploader = this; //vital
        }
        
        public void Dispose()
        {
            _nativeFileUploaderProxy?.Dispose();

            GC.SuppressFinalize(this);
        }
        
        public bool TrySetContext(object context) => _nativeFileUploaderProxy?.TrySetContext(context) ?? false;
        public bool TrySetBluetoothDevice(object bluetoothDevice) => _nativeFileUploaderProxy?.TrySetContext(bluetoothDevice) ?? false;
        public bool TryInvalidateCachedTransport() => _nativeFileUploaderProxy?.TryInvalidateCachedTransport() ?? false;

        public EFileUploaderVerdict BeginUpload(
            string remoteFilePath,
            byte[] data,

            int? pipelineDepth = null,
            int? byteAlignment = null,

            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        )
        {
            data = data ?? throw new ArgumentNullException(nameof(data));
            
            RemoteFilePathHelpers.ValidateRemoteFilePath(remoteFilePath); //                    order
            remoteFilePath = RemoteFilePathHelpers.SanitizeRemoteFilePath(remoteFilePath); //   order

            var verdict = _nativeFileUploaderProxy.BeginUpload(
                data: data,
                remoteFilePath: remoteFilePath,

                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,

                initialMtuSize: initialMtuSize,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );

            return verdict;
        }
        
        public void Cancel() => _nativeFileUploaderProxy?.Cancel();
        public void Disconnect() => _nativeFileUploaderProxy?.Disconnect();
        public void CleanupResourcesOfLastUpload() => _nativeFileUploaderProxy?.CleanupResourcesOfLastUpload();
        
        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<FileUploadedEventArgs> _fileUploaded;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
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

        public event EventHandler<LogEmittedEventArgs> LogEmitted
        {
            add
            {
                _logEmitted -= value;
                _logEmitted += value;
            }
            remove => _logEmitted -= value;
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
        
        /// <summary>Event raised when a specific file gets uploaded successfully</summary>
        public event EventHandler<FileUploadedEventArgs> FileUploaded
        {
            add
            {
                _fileUploaded -= value;
                _fileUploaded += value;
            }
            remove => _fileUploaded -= value;
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
            IDictionary<string, TData> remoteFilePathsAndTheirData,
            int sleepTimeBetweenRetriesInMs = 100,
            int timeoutPerUploadInMs = -1,
            int maxTriesPerUpload = 10,
            bool moveToNextUploadInCaseOfError = true,
            bool autodisposeStreams = false,
            
            int? pipelineDepth = null,
            int? byteAlignment = null,

            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        ) where TData : notnull
        {
            RemoteFilePathHelpers.ValidateRemoteFilePathsWithDataBytes(remoteFilePathsAndTheirData);
            var sanitizedRemoteFilePathsAndTheirData = RemoteFilePathHelpers.SanitizeRemoteFilePathsWithData(remoteFilePathsAndTheirData);

            var filesThatFailedToBeUploaded = new List<string>(2);

            foreach (var x in sanitizedRemoteFilePathsAndTheirData)
            {
                try
                {
                    await UploadAsync(
                        data: x.Value,
                        remoteFilePath: x.Key,

                        maxTriesCount: maxTriesPerUpload,
                        autodisposeStream: autodisposeStreams,
                        timeoutForUploadInMs: timeoutPerUploadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,

                        pipelineDepth: pipelineDepth,
                        byteAlignment: byteAlignment,
                        
                        initialMtuSize: initialMtuSize,
                        windowCapacity: windowCapacity,
                        memoryAlignment: memoryAlignment
                    );
                }
                catch (UploadErroredOutException)
                {
                    if (moveToNextUploadInCaseOfError) //00
                    {
                        filesThatFailedToBeUploaded.Add(x.Key);
                        continue;
                    }

                    throw;
                }
            }

            return filesThatFailedToBeUploaded;

            //00  we prefer to upload as many files as possible and report any failures collectively at the very end   we resorted to this
            //    tactic because failures are fairly common when uploading 50 files or more over to aed devices, and we wanted to ensure
            //    that it would be as easy as possible to achieve the mass uploading just by using the default settings 
        }
        
        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;
        public async Task UploadAsync<TData>(
            TData data,
            string remoteFilePath,
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
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            
            if (maxTriesCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTriesCount), maxTriesCount, "Must be greater than zero");
            
            var dataArray = await GetDataAsByteArray_(data, autodisposeStream);
            
            gracefulCancellationTimeoutInMs = gracefulCancellationTimeoutInMs >= 0 //we want to ensure that the timeout is always sane
                ? gracefulCancellationTimeoutInMs
                : DefaultGracefulCancellationTimeoutInMs;

            var isCancellationRequested = false;
            var fileUploadProgressEventsCount = 0;
            var suspiciousTransportFailuresCount = 0;
            var didWarnOnceAboutUnstableConnection = false;
            for (var triesCount = 1; !isCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSource<bool>(state: false);
                try
                {
                    Cancelled += FileUploader_Cancelled_;
                    FileUploaded += FileUploader_FileUploaded_;
                    StateChanged += FileUploader_StateChanged_;
                    FatalErrorOccurred += FileUploader_FatalErrorOccurred_;
                    FileUploadProgressPercentageAndDataThroughputChanged += FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_;

                    var failSafeSettingsToApply = GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable_(
                        triesCount_: triesCount,
                        maxTriesCount_: maxTriesCount,
                        suspiciousTransportFailuresCount_: suspiciousTransportFailuresCount,
                        emitWarningAboutUnstableConnection_: !didWarnOnceAboutUnstableConnection
                    );
                    if (failSafeSettingsToApply != null)
                    {
                        didWarnOnceAboutUnstableConnection = true;
                        byteAlignment = failSafeSettingsToApply.Value.byteAlignment;
                        pipelineDepth = failSafeSettingsToApply.Value.pipelineDepth;
                        initialMtuSize = failSafeSettingsToApply.Value.initialMtuSize;
                        windowCapacity = failSafeSettingsToApply.Value.windowCapacity;
                        memoryAlignment = failSafeSettingsToApply.Value.memoryAlignment;
                    }

                    var verdict = BeginUpload( //00 dont use task.run here for now
                        data: dataArray,
                        remoteFilePath: remoteFilePath,
                        
                        pipelineDepth: pipelineDepth, //      ios only
                        byteAlignment: byteAlignment, //      ios only

                        initialMtuSize: initialMtuSize, //    android only
                        windowCapacity: windowCapacity, //    android only
                        memoryAlignment: memoryAlignment //   android only
                    );
                    if (verdict != EFileUploaderVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    _ = timeoutForUploadInMs < 0
                        ? await taskCompletionSource.Task
                        : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutForUploadInMs); //order

                    break;
                }
                catch (TimeoutException ex)
                {
                    //todo   silently cancel the upload here on best effort basis
                    
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        resource: remoteFilePath,
                        oldState: EFileUploaderState.None, //better not use this.State here because the native call might fail
                        newState: EFileUploaderState.Error
                    ));

                    throw new UploadTimeoutException(remoteFilePath, timeoutForUploadInMs, ex);
                }
                catch (UploadErroredOutException ex) //errors with code in_value(3) and even UnauthorizedException happen all the time in android when multiuploading files
                {
                    if (fileUploadProgressEventsCount <= 10)
                    {
                        suspiciousTransportFailuresCount++;
                    }
                    
                    if (ex is UploadErroredOutRemoteFolderNotFoundException) //order    no point to retry if any of the remote parent folders are not there
                        throw;

                    if (++triesCount > maxTriesCount) //order
                        throw new AllUploadAttemptsFailedException(remoteFilePath, maxTriesCount, innerException: ex);

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
                        resource: remoteFilePath,
                        oldState: EFileUploaderState.None,
                        newState: EFileUploaderState.Error
                    ));

                    // OnFatalErrorOccurred(); //better not   too much fuss
                    
                    throw new UploadInternalErrorException(remoteFilePath, ex);
                }
                finally
                {
                    Cancelled -= FileUploader_Cancelled_;
                    FileUploaded -= FileUploader_FileUploaded_;
                    StateChanged -= FileUploader_StateChanged_;
                    FatalErrorOccurred -= FileUploader_FatalErrorOccurred_;
                    FileUploadProgressPercentageAndDataThroughputChanged -= FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_;
                    
                    CleanupResourcesOfLastUpload(); //vital
                }

                void FileUploader_Cancelled_(object _, CancelledEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(new UploadCancelledException());
                }
                
                void FileUploader_FileUploaded_(object _, FileUploadedEventArgs ea_)
                {
                    taskCompletionSource.TrySetResult(true);
                }

                // ReSharper disable AccessToModifiedClosure
                void FileUploader_StateChanged_(object _, StateChangedEventArgs ea_)
                {
                    switch (ea_.NewState)
                    {
                        case EFileUploaderState.Idle:
                            fileUploadProgressEventsCount = 0; //its vital to reset the counter here to account for retries
                            return;
                        
                        case EFileUploaderState.Complete:
                            //taskCompletionSource.TrySetResult(true); //dont   we want to wait for the FileUploaded event
                            return;

                        case EFileUploaderState.Cancelling: //20
                            if (isCancellationRequested)
                                return;

                            isCancellationRequested = true;

                            Task.Run(async () =>
                            {
                                try
                                {
                                    if (gracefulCancellationTimeoutInMs > 0) //keep this check here to avoid unnecessary task rescheduling
                                    {
                                        await Task.Delay(gracefulCancellationTimeoutInMs);                                        
                                    }

                                    OnCancelled(new CancelledEventArgs()); //00
                                }
                                catch // (Exception ex)
                                {
                                    // ignored
                                }
                            });
                            return;
                    }
                    
                    //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see OnCancelled()
                    //    getting called right above   but if that takes too long we give the killing blow by calling OnCancelled() manually here
                }

                void FileUploader_FileUploadProgressPercentageAndDataThroughputChanged_(object sender, FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
                {
                    fileUploadProgressEventsCount++;
                }

                void FileUploader_FatalErrorOccurred_(object sender, FatalErrorOccurredEventArgs ea)
                {
                    var isAboutUnauthorized = ea.ErrorCode == EMcuMgrErrorCode.AccessDenied;
                    if (isAboutUnauthorized)
                    {
                        taskCompletionSource.TrySetException(new UploadUnauthorizedException( //specific case
                            remoteFilePath: remoteFilePath,
                            mcuMgrErrorCode: ea.ErrorCode,
                            groupReturnCode: ea.GroupReturnCode,
                            nativeErrorMessage: ea.ErrorMessage
                        ));
                        return;
                    }
                    
                    var isAboutFolderNotExisting = ea.ErrorCode == EMcuMgrErrorCode.Unknown;
                    if (isAboutFolderNotExisting)
                    {
                        taskCompletionSource.TrySetException(new UploadErroredOutRemoteFolderNotFoundException( //specific case
                            remoteFilePath: remoteFilePath,
                            mcuMgrErrorCode: ea.ErrorCode,
                            groupReturnCode: ea.GroupReturnCode,
                            nativeErrorMessage: ea.ErrorMessage
                        ));
                        return;
                    }

                    taskCompletionSource.TrySetException(new UploadErroredOutException( //generic
                        remoteFilePath: remoteFilePath,
                        mcuMgrErrorCode: ea.ErrorCode,
                        groupReturnCode: ea.GroupReturnCode,
                        nativeErrorMessage: ea.ErrorMessage
                    ));
                }
                // ReSharper restore AccessToModifiedClosure
            }
            
            if (isCancellationRequested) //vital
                throw new UploadCancelledException(); //20

            return;

            (int? byteAlignment, int? pipelineDepth, int? initialMtuSize, int? windowCapacity, int? memoryAlignment)? GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable_(
                int triesCount_,
                int maxTriesCount_,
                int suspiciousTransportFailuresCount_,
                bool emitWarningAboutUnstableConnection_
            )
            {
                var isConnectionTooUnstableForUploading_ = triesCount_ >= 2 && (triesCount_ == maxTriesCount_ || triesCount_ >= 3 && suspiciousTransportFailuresCount_ >= 2);
                if (!isConnectionTooUnstableForUploading_)
                    return null;

                var byteAlignment_ = AppleTidbits.FailSafeBleConnectionSettings.ByteAlignment; //        ios + maccatalyst
                var pipelineDepth_ = AppleTidbits.FailSafeBleConnectionSettings.PipelineDepth; //        ios + maccatalyst
                var initialMtuSize_ = AndroidTidbits.FailSafeBleConnectionSettings.InitialMtuSize; //    android    when noticing persistent failures when uploading we resort
                var windowCapacity_ = AndroidTidbits.FailSafeBleConnectionSettings.WindowCapacity; //    android    to forcing the most failsafe settings we know of just in case
                var memoryAlignment_ = AndroidTidbits.FailSafeBleConnectionSettings.MemoryAlignment; //  android    we manage to salvage this situation (works with SamsungA8 android tablets)

                if (emitWarningAboutUnstableConnection_)
                {
                    OnLogEmitted(new LogEmittedEventArgs(
                        level: ELogLevel.Warning,
                        message: $"[FU.UA.GFCSICPTBU.010] Attempt#{triesCount_}: Connection is too unstable for uploading assets to the target device. Subsequent tries will use failsafe parameters on the connection " +
                                 $"just in case it helps (byteAlignment={byteAlignment_}, pipelineDepth={pipelineDepth_}, initialMtuSize={initialMtuSize_}, windowCapacity={windowCapacity_}, memoryAlignment={memoryAlignment_})",
                        resource: "File",
                        category: "FileUploader"
                    ));
                }

                return (byteAlignment: byteAlignment_, pipelineDepth: pipelineDepth_, initialMtuSize: initialMtuSize_, windowCapacity: windowCapacity_, memoryAlignment: memoryAlignment_);
            }
            
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

        void IFileUploaderEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea);
        void IFileUploaderEventEmittable.OnLogEmitted(LogEmittedEventArgs ea) => OnLogEmitted(ea);
        void IFileUploaderEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFileUploaderEventEmittable.OnFileUploaded(FileUploadedEventArgs ea) => OnFileUploaded(ea);
        void IFileUploaderEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFileUploaderEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFileUploaderEventEmittable.OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFileUploadProgressPercentageAndDataThroughputChanged(ea);

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnFileUploaded(FileUploadedEventArgs ea) => _fileUploaded?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        private void OnFileUploadProgressPercentageAndDataThroughputChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileUploadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);

        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFileUploaderProxy
        internal class GenericNativeFileUploaderCallbacksProxy : INativeFileUploaderCallbacksProxy
        {
            public IFileUploaderEventEmittable FileUploader { get; set; }

            public void CancelledAdvertisement()
                => FileUploader?.OnCancelled(new CancelledEventArgs());

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => FileUploader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                ));

            public void StateChangedAdvertisement(string resource, EFileUploaderState oldState, EFileUploaderState newState)
                => FileUploader?.OnStateChanged(new StateChangedEventArgs(
                    resource: resource,
                    newState: newState,
                    oldState: oldState
                ));

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FileUploader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FileUploadedAdvertisement(string resource)
                => FileUploader?.OnFileUploaded(new FileUploadedEventArgs(resource));

            public void FatalErrorOccurredAdvertisement(
                string resource,
                string errorMessage,
                EMcuMgrErrorCode mcuMgrErrorCode,
                EFileUploaderGroupReturnCode fileUploaderGroupReturnCode
            ) => FileUploader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(
                resource,
                errorMessage,
                mcuMgrErrorCode,
                fileUploaderGroupReturnCode
            ));

            public void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                int progressPercentage,
                float averageThroughput
            ) => FileUploader?.OnFileUploadProgressPercentageAndDataThroughputChanged(new FileUploadProgressPercentageAndDataThroughputChangedEventArgs(
                averageThroughput: averageThroughput,
                progressPercentage: progressPercentage
            ));
        }
    }
}