// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;

namespace Laerdal.McuMgr.FileDownloader
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
        
        public void Dispose()
        {
            _nativeFileDownloaderProxy?.Dispose();

            GC.SuppressFinalize(this);
        }

        public string LastFatalErrorMessage => _nativeFileDownloaderProxy?.LastFatalErrorMessage;

        public EFileDownloaderVerdict BeginDownload(
            string remoteFilePath,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        )
        {
            RemoteFilePathHelpers.ValidateRemoteFilePath(remoteFilePath); //                    order
            remoteFilePath = RemoteFilePathHelpers.SanitizeRemoteFilePath(remoteFilePath); //   order

            var verdict = _nativeFileDownloaderProxy.BeginDownload(
                remoteFilePath: remoteFilePath,
                initialMtuSize: initialMtuSize
            );

            return verdict;
        }

        public void Cancel() => _nativeFileDownloaderProxy?.Cancel();
        public void Disconnect() => _nativeFileDownloaderProxy?.Disconnect();

        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<DownloadCompletedEventArgs> _downloadCompleted;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
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
            int timeoutPerDownloadInMs = -1,
            int maxRetriesPerDownload = 10,
            int sleepTimeBetweenRetriesInMs = 0,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        )
        {
            RemoteFilePathHelpers.ValidateRemoteFilePaths(remoteFilePaths); //                                        order
            var sanitizedUniqueRemoteFilesPaths = RemoteFilePathHelpers.SanitizeRemoteFilePaths(remoteFilePaths); //  order

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

                        maxTriesCount: maxRetriesPerDownload,
                        timeoutForDownloadInMs: timeoutPerDownloadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,

                        initialMtuSize: initialMtuSize,
                        windowCapacity: windowCapacity,
                        memoryAlignment: memoryAlignment
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
            int timeoutForDownloadInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int gracefulCancellationTimeoutInMs = DefaultGracefulCancellationTimeoutInMs,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        )
        {
            if (maxTriesCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTriesCount), maxTriesCount, "Must be greater than zero!");
            
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
                var taskCompletionSource = new TaskCompletionSource<byte[]>(state: null);

                try
                {
                    Cancelled += FileDownloader_Cancelled_;
                    StateChanged += FileDownloader_StateChanged_;
                    DownloadCompleted += FileDownloader_DownloadCompleted_;
                    FatalErrorOccurred += FileDownloader_FatalErrorOccurred_;
                    FileDownloadProgressPercentageAndDataThroughputChanged += FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_;
                    
                    var failSafeSettingsToApply = GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable_(
                        triesCount_: triesCount,
                        maxTriesCount_: maxTriesCount,
                        suspiciousTransportFailuresCount_: suspiciousTransportFailuresCount,
                        emitWarningAboutUnstableConnection_: !didWarnOnceAboutUnstableConnection
                    );
                    if (failSafeSettingsToApply != null)
                    {
                        didWarnOnceAboutUnstableConnection = true;
                        initialMtuSize = failSafeSettingsToApply.Value.initialMtuSize;
                        windowCapacity = failSafeSettingsToApply.Value.windowCapacity;
                        memoryAlignment = failSafeSettingsToApply.Value.memoryAlignment;
                    }

                    var verdict = BeginDownload( //00 dont use task.run here for now
                        remoteFilePath: remoteFilePath,

                        initialMtuSize: initialMtuSize,
                        windowCapacity: windowCapacity,
                        memoryAlignment: memoryAlignment
                    );
                    if (verdict != EFileDownloaderVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    result = timeoutForDownloadInMs < 0
                        ? await taskCompletionSource.Task
                        : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutForDownloadInMs);

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
                    if (ex is DownloadErroredOutRemoteFileNotFoundException) //order   no point to retry if the remote file is not there
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

                void FileDownloader_FileDownloadProgressPercentageAndDataThroughputChanged_(object sender, FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea)
                {
                    fileDownloadProgressEventsCount++;
                }

                void FileDownloader_DownloadCompleted_(object sender_, DownloadCompletedEventArgs ea_)
                {
                    taskCompletionSource.TrySetResult(ea_.Data);
                }

                void FileDownloader_FatalErrorOccurred_(object sender_, FatalErrorOccurredEventArgs ea_)
                {
                    var isAboutUnauthorized = ea_.ErrorMessage?.ToUpperInvariant().Contains("UNRECOGNIZED (11)", StringComparison.InvariantCultureIgnoreCase) ?? false;
                    if (isAboutUnauthorized)
                    {
                        taskCompletionSource.TrySetException(new UnauthorizedException(
                            resource: remoteFilePath,
                            nativeErrorMessage: ea_.ErrorMessage
                        ));
                        return;
                    }
                    
                    var isAboutRemoteFileNotFound = ea_.ErrorMessage
                        ?.ToUpperInvariant()
                        .Replace("NO_ENTRY (5)", "NO ENTRY (5)") //normalize the error for android so that it will be the same as in ios
                        .Contains("NO ENTRY (5)") ?? false;
                    if (isAboutRemoteFileNotFound)
                    {
                        taskCompletionSource.TrySetException(new DownloadErroredOutRemoteFileNotFoundException(remoteFilePath)); //specific case
                        return;
                    }

                    taskCompletionSource.TrySetException(new DownloadErroredOutException(ea_.ErrorMessage)); //generic
                }
            }

            if (isCancellationRequested) //vital
                throw new DownloadCancelledException(); //20

            return result;

            (int? initialMtuSize, int? windowCapacity, int? memoryAlignment)? GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable_(
                int triesCount_,
                int maxTriesCount_,
                int suspiciousTransportFailuresCount_,
                bool emitWarningAboutUnstableConnection_
            )
            {
                var isConnectionTooUnstableForDownloading_ = triesCount_ >= 2 && (triesCount_ == maxTriesCount_ || triesCount_ >= 3 && suspiciousTransportFailuresCount_ >= 2);
                if (!isConnectionTooUnstableForDownloading_)
                    return null;

                var initialMtuSize_ = AndroidTidbits.FailSafeBleConnectionSettings.InitialMtuSize; //    android    when noticing persistent failures when uploading we resort
                var windowCapacity_ = AndroidTidbits.FailSafeBleConnectionSettings.WindowCapacity; //    android    to forcing the most failsafe settings we know of just in case
                var memoryAlignment_ = AndroidTidbits.FailSafeBleConnectionSettings.MemoryAlignment; //  android    we manage to salvage this situation (works with SamsungA8 android tablets)

                if (emitWarningAboutUnstableConnection_)
                {
                    OnLogEmitted(new LogEmittedEventArgs(
                        level: ELogLevel.Warning,
                        message: $"[FD.DA.GFCSICPTBU.010] Attempt#{triesCount_}: Connection is too unstable for uploading assets to the target device. Subsequent tries will use failsafe parameters on the connection " +
                                 $"just in case it helps (initialMtuSize={initialMtuSize_}, windowCapacity={windowCapacity_}, memoryAlignment={memoryAlignment_})",
                        resource: "File",
                        category: "FileDownloader"
                    ));
                }

                return (initialMtuSize: initialMtuSize_, windowCapacity: windowCapacity_, memoryAlignment: memoryAlignment_);
            }
            
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

        void IFileDownloaderEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea); //just to make the class unit-test friendly without making the methods public
        void IFileDownloaderEventEmittable.OnLogEmitted(LogEmittedEventArgs ea) => OnLogEmitted(ea);
        void IFileDownloaderEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFileDownloaderEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFileDownloaderEventEmittable.OnDownloadCompleted(DownloadCompletedEventArgs ea) => OnDownloadCompleted(ea);
        void IFileDownloaderEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFileDownloaderEventEmittable.OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFileDownloadProgressPercentageAndDataThroughputChanged(ea);
        
        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnDownloadCompleted(DownloadCompletedEventArgs ea) => _downloadCompleted?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnFileDownloadProgressPercentageAndDataThroughputChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileDownloadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);

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

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage)
                => FileDownloader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(resource, errorMessage));

            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
                => FileDownloader?.OnFileDownloadProgressPercentageAndDataThroughputChanged(new FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
        }
    }
}