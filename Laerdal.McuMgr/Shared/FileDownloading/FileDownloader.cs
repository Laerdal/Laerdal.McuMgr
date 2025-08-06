// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.FileDownloading
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader, IFileDownloaderEventEmittable
    {
        private readonly INativeFileDownloaderProxy _nativeFileDownloaderProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileDownloaderProxy
        internal FileDownloader(INativeFileDownloaderProxy nativeFileDownloaderProxy)
        {
            _nativeFileDownloaderProxy = nativeFileDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderProxy));
            _nativeFileDownloaderProxy.FileDownloader = this; //vital
        }

        private bool _disposed;
        public void Dispose()
        {
            Dispose(isDisposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
                return;

            if (!isDisposing)
                return;

            try
            {
                _nativeFileDownloaderProxy?.Dispose();
            }
            catch
            {
                //ignored
            }

            _disposed = true;
        }

        public string LastFatalErrorMessage => _nativeFileDownloaderProxy?.LastFatalErrorMessage;

        public EFileDownloaderVerdict BeginDownload(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize = null,
            int? windowCapacity = null //not applicable currently   but nordic considers these for future use
        )
        {
            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

            remoteFilePath = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePath(remoteFilePath);

            var failsafeConnectionSettings = ConnectionSettingsHelpers.GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
                initialMtuSize: initialMtuSize,
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,
                uploadingNotDownloading: false
            );
            if (failsafeConnectionSettings != null)
            {
                initialMtuSize = failsafeConnectionSettings.Value.initialMtuSize;
                // windowCapacity = failsafeConnectionSettings.Value.windowCapacity;
                // memoryAlignment = failsafeConnectionSettings.Value.memoryAlignment;
                
                OnLogEmitted(new LogEmittedEventArgs(
                    level: ELogLevel.Warning,
                    message: $"[FD.BD.010] Host device '{hostDeviceModel} (made by {hostDeviceManufacturer})' is known to be problematic. Resorting to using failsafe settings (initialMtuSize={initialMtuSize})",
                    resource: "File",
                    category: "FileDownloader"
                ));
            }
            
            var verdict = _nativeFileDownloaderProxy.BeginDownload(
                remoteFilePath: remoteFilePath,
                initialMtuSize: initialMtuSize
            );

            return verdict;
        }

        public void Cancel() => _nativeFileDownloaderProxy?.Cancel();
        public void Disconnect() => _nativeFileDownloaderProxy?.Disconnect();

        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<DownloadCompletedEventArgs> _downloadCompleted;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs> _fileDownloadProgressPercentageAndDataThroughputChanged;

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

        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted
        {
            add
            {
                _downloadCompleted -= value;
                _downloadCompleted += value;
            }
            remove => _downloadCompleted -= value;
        }

        public event EventHandler<FileDownloadProgressPercentageAndDataThroughputChangedEventArgs> FileDownloadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _fileDownloadProgressPercentageAndDataThroughputChanged -= value;
                _fileDownloadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _fileDownloadProgressPercentageAndDataThroughputChanged -= value;
        }

        public async Task<IDictionary<string, byte[]>> DownloadAsync(
            IEnumerable<string> remoteFilePaths,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutPerDownloadInMs = -1,
            int maxTriesPerDownload = 10,
            int sleepTimeBetweenRetriesInMs = 0,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        )
        {
            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

            var sanitizedUniqueRemoteFilesPaths = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePaths(remoteFilePaths);

            var results = sanitizedUniqueRemoteFilesPaths.ToDictionary(
                keySelector: x => x,
                elementSelector: _ => (byte[])null
            );

            foreach (var path in sanitizedUniqueRemoteFilesPaths) //00 impossible to parallelize
            {
                try
                {
                    var data = await DownloadAsync(
                        remoteFilePath: path,
                        hostDeviceModel: hostDeviceModel,
                        hostDeviceManufacturer: hostDeviceManufacturer,

                        maxTriesCount: maxTriesPerDownload,
                        timeoutForDownloadInMs: timeoutPerDownloadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,

                        initialMtuSize: initialMtuSize,
                        windowCapacity: windowCapacity
                    );

                    results[path] = data;
                }
                catch (DownloadErroredOutException) //10
                {
                }
            }

            return results;
            
            //00  we would love to parallelize all this but the native side simply reverts to queuing the requests so its pointless
            //
            //10  we dont want to throw here because we want to return the results for the files that were successfully downloaded
            //    if a file fails to download we simply return null data for that file
        }

        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;
        public async Task<byte[]> DownloadAsync(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int timeoutForDownloadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = 2_500,
            int? initialMtuSize = null,
            int? windowCapacity = null
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
            
            var result = (byte[])null;
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
                    StateChanged += FileDownloader_StateChanged_;
                    DownloadCompleted += FileDownloader_DownloadCompleted_;
                    FatalErrorOccurred += FileDownloader_FatalErrorOccurred_;
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
                                message: $"[FD.DA.010] Attempt#{triesCount}: Connection is too unstable for downloading assets from the target device. Subsequent tries will use failsafe parameters on the connection " +
                                         $"just in case it helps (initialMtuSize={initialMtuSize?.ToString() ?? "null"}, windowCapacity={windowCapacity?.ToString() ?? "null"})",
                                resource: "File",
                                category: "FileDownloader"
                            ));
                        }
                    }

                    var verdict = BeginDownload( //00 dont use task.run here for now
                        remoteFilePath: remoteFilePath,
                        hostDeviceModel: hostDeviceModel,
                        hostDeviceManufacturer: hostDeviceManufacturer,

                        initialMtuSize: initialMtuSize,
                        windowCapacity: windowCapacity
                    );
                    if (verdict != EFileDownloaderVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    result = await taskCompletionSource.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeoutForDownloadInMs);
                    break;
                }
                catch (TimeoutException ex)
                {
                    //todo   silently cancel the download here on best effort basis

                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        resource: remoteFilePath,
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
                        resource: remoteFilePath,
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
                    StateChanged -= FileDownloader_StateChanged_;
                    DownloadCompleted -= FileDownloader_DownloadCompleted_;
                    FatalErrorOccurred -= FileDownloader_FatalErrorOccurred_;
                    FileDownloadProgressPercentageAndDataThroughputChanged -= FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_;
                }

                void FileDownloader_Cancelled_(object sender_, CancelledEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(new DownloadCancelledException());
                }

                void FileDownloader_StateChanged_(object sender_, StateChangedEventArgs ea_)
                {
                    switch (ea_.NewState)
                    {
                        case EFileDownloaderState.Idle:
                            fileDownloadProgressEventsCount = 0;
                            return;
                        
                        case EFileDownloaderState.Cancelling:
                            if (isCancellationRequested)
                                return;
                            
                            isCancellationRequested = true;
                            Task.Run(async () =>
                            {
                                try
                                {
                                    if (gracefulCancellationTimeoutInMs > 0) //keep this check here to avoid unnecessary context switching
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

                void FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_(object _, FileDownloadProgressPercentageAndDataThroughputChangedEventArgs __)
                {
                    fileDownloadProgressEventsCount++;
                }

                void FileDownloader_DownloadCompleted_(object _, DownloadCompletedEventArgs ea_)
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
                throw new DownloadCancelledException(); //20

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

        void ILogEmittable.OnLogEmitted(in LogEmittedEventArgs ea) => OnLogEmitted(in ea);
        void IFileDownloaderEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea); //just to make the class unit-test friendly without making the methods public
        void IFileDownloaderEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFileDownloaderEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFileDownloaderEventEmittable.OnDownloadCompleted(DownloadCompletedEventArgs ea) => OnDownloadCompleted(ea);
        void IFileDownloaderEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFileDownloadProgressPercentageAndDataThroughputChanged(ea);
        
        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnLogEmitted(in LogEmittedEventArgs ea) => _logEmitted?.InvokeAndIgnoreExceptions(this, ea); // in the special case of log-emitted we prefer the .invoke() flavour for the sake of performance
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnDownloadCompleted(DownloadCompletedEventArgs ea) => _downloadCompleted?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        private void OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileDownloadProgressPercentageAndDataThroughputChanged?.InvokeAndIgnoreExceptions(this, ea);
        
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea)
        {
            OnLogEmitted(new LogEmittedEventArgs(
                level: ELogLevel.Error,
                message: $"[{nameof(ea.GlobalErrorCode)}='{ea.GlobalErrorCode}'] {ea.ErrorMessage}",
                resource: ea.Resource,
                category: "file-downloader"
            ));

            OnFatalErrorOccurred_(ea);
            return;

            void OnFatalErrorOccurred_(FatalErrorOccurredEventArgs ea_)
            {
                _fatalErrorOccurred?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea_);
            }
        }

        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFileDownloaderProxy
        internal class GenericNativeFileDownloaderCallbacksProxy : INativeFileDownloaderCallbacksProxy
        {
            public IFileDownloaderEventEmittable FileDownloader { get; set; }

            public void CancelledAdvertisement()
                => FileDownloader?.OnCancelled(new CancelledEventArgs());

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => FileDownloader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                ));

            public void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState)
                => FileDownloader?.OnStateChanged(new StateChangedEventArgs(
                    resource: resource,
                    newState: newState,
                    oldState: oldState
                ));

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FileDownloader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void DownloadCompletedAdvertisement(string resource, byte[] data)
                => FileDownloader?.OnDownloadCompleted(new DownloadCompletedEventArgs(resource, data));

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EGlobalErrorCode globalErrorCode)
                => FileDownloader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(resource, errorMessage, globalErrorCode));

            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float currentThroughputInKbps, float totalAverageThroughputInKbps)
                => FileDownloader?.OnFileDownloadProgressPercentageAndDataThroughputChanged(new FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(
                    progressPercentage: progressPercentage,
                    currentThroughputInKbps: currentThroughputInKbps,
                    totalAverageThroughputInKbps: totalAverageThroughputInKbps
                ));
        }
    }
}