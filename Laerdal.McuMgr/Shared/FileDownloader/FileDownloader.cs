// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader, IFileDownloaderEventEmitters
    {
        private readonly INativeFileDownloaderProxy _nativeFileDownloaderProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileDownloaderProxy
        internal FileDownloader(INativeFileDownloaderProxy nativeFileDownloaderProxy)
        {
            _nativeFileDownloaderProxy = nativeFileDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderProxy));
            _nativeFileDownloaderProxy.FileDownloader = this; //vital
        }

        public string LastFatalErrorMessage => _nativeFileDownloaderProxy?.LastFatalErrorMessage;

        public EFileDownloaderVerdict BeginDownload(string remoteFilePath)
        {           
            if (string.IsNullOrWhiteSpace(remoteFilePath))
                throw new ArgumentException($"The {nameof(remoteFilePath)} parameter is dud!");

            remoteFilePath = remoteFilePath.Trim();
            if (remoteFilePath.EndsWith("/")) //00
                throw new ArgumentException($"The given {nameof(remoteFilePath)} points to a directory not a file!");

            if (!remoteFilePath.StartsWith("/")) //10
            {
                remoteFilePath = $"/{remoteFilePath}";
            }

            var verdict = _nativeFileDownloaderProxy.BeginDownload(remoteFilePath: remoteFilePath);

            return verdict;

            //00  we spot this very common mistake and stop it right here    otherwise it causes a very cryptic error
            //10  the target file path must be absolute   if its not then we make it so   relative paths cause exotic errors
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
            int sleepTimeBetweenRetriesInMs = 0
        )
        {
            remoteFilePaths = remoteFilePaths ?? throw new ArgumentNullException(nameof(remoteFilePaths));
            
            var sanitizedRemoteFilesPaths = remoteFilePaths
                .GroupBy(p => p) //unique ones only   todo   normalize the paths here
                .Select(p => p.First())
                .ToArray();
            
            if (sanitizedRemoteFilesPaths.Any(s => string.IsNullOrWhiteSpace(s) || s.EndsWith("/")))
                throw new ArgumentException($"The {nameof(remoteFilePaths)} parameter contains duds and/or paths that end with '/'!", nameof(remoteFilePaths));

            var results = new Dictionary<string, byte[]>(sanitizedRemoteFilesPaths.Length);
            foreach (var path in sanitizedRemoteFilesPaths) //00 impossible to parallelize
            {
                if (results.ContainsKey(path)) //already processed
                    continue;
                
                try
                {
                    var data = await DownloadAsync(
                        remoteFilePath: path,
                        maxRetriesCount: maxRetriesPerDownload,
                        timeoutForDownloadInMs: timeoutPerDownloadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
                    );

                    results[path] = data;
                }
                catch (DownloadErroredOutRemoteFileNotFoundException)
                {
                    continue;
                }
            }

            return results;
            
            //0 we would love to parallelize all this but the native side simply reverts to queuing the requests so its pointless
        }

        public async Task<byte[]> DownloadAsync(
            string remoteFilePath,
            int timeoutForDownloadInMs = -1,
            int maxRetriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000
        )
        {
            var result = (byte[])null;
            var isCancellationRequested = false;
            for (var retry = 0; !isCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSource<byte[]>(state: null);

                try
                {
                    Cancelled += DownloadAsyncOnCancelled;
                    StateChanged += DownloadAsyncOnStateChanged;
                    DownloadCompleted += DownloadAsyncOnDownloadCompleted;
                    FatalErrorOccurred += DownloadAsyncOnFatalErrorOccurred;

                    var verdict = BeginDownload(remoteFilePath); //00 dont use task.run here for now
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
                    
                    (this as IFileDownloaderEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                        resource: remoteFilePath,
                        oldState: EFileDownloaderState.None, //better not use this.State here because the native call might fail
                        newState: EFileDownloaderState.Error
                    ));

                    throw new DownloadTimeoutException(remoteFilePath, timeoutForDownloadInMs, ex);
                }
                catch (DownloadErroredOutException ex)
                {
                    if (ex is DownloadErroredOutRemoteFileNotFoundException) //no point to retry if the remote file is not there
                        throw;

                    if (++retry > maxRetriesCount)
                        throw new DownloadErroredOutException($"Failed to download '{remoteFilePath}' after trying {maxRetriesCount + 1} times", innerException: ex);

                    if (sleepTimeBetweenRetriesInMs > 0)
                    {
                        await Task.Delay(sleepTimeBetweenRetriesInMs);
                    }

                    continue;
                }
                catch (Exception ex) when (
                    !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                    && !(ex is TimeoutException)
                    && !(ex is IDownloadRelatedException) //this accounts for both cancellations and download exceptions!
                )
                {
                    (this as IFileDownloaderEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                        resource: remoteFilePath,
                        oldState: EFileDownloaderState.None,
                        newState: EFileDownloaderState.Error
                    ));

                    //OnFatalErrorOccurred(); //not worth it in this case  

                    throw new DownloadErroredOutException(ex.Message, ex);
                }
                finally
                {
                    Cancelled -= DownloadAsyncOnCancelled;
                    StateChanged -= DownloadAsyncOnStateChanged;
                    DownloadCompleted -= DownloadAsyncOnDownloadCompleted;
                    FatalErrorOccurred -= DownloadAsyncOnFatalErrorOccurred;
                }

                void DownloadAsyncOnCancelled(object sender, CancelledEventArgs ea)
                {
                    taskCompletionSource.TrySetException(new DownloadCancelledException());
                }

                void DownloadAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
                {
                    if (ea.NewState != EFileDownloaderState.Cancelling || isCancellationRequested)
                        return;

                    isCancellationRequested = true;

                    Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(5_000); 
                            taskCompletionSource.TrySetException(new DownloadCancelledException()); //00
                        }
                        catch // (Exception ex)
                        {
                            // ignored
                        }
                    });

                    return;
                    
                    //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see
                    //    DownloadAsyncOnCancelled() getting called right above   but if that takes too long we give the killing blow manually
                }

                void DownloadAsyncOnDownloadCompleted(object sender, DownloadCompletedEventArgs ea)
                {
                    taskCompletionSource.TrySetResult(ea.Data);
                }

                void DownloadAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
                {
                    var isAboutRemoteFileNotFound = ea.ErrorMessage
                        ?.ToUpperInvariant()
                        .Replace("NO_ENTRY (5)", "NO ENTRY (5)") //normalize the error for android so that it will be the same as in ios
                        .Contains("NO ENTRY (5)") ?? false;
                    if (isAboutRemoteFileNotFound)
                    {
                        taskCompletionSource.TrySetException(new DownloadErroredOutRemoteFileNotFoundException(remoteFilePath)); //specific case
                        return;
                    }

                    taskCompletionSource.TrySetException(new DownloadErroredOutException(ea.ErrorMessage)); //generic
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

        void IFileDownloaderEventEmitters.OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        void IFileDownloaderEventEmitters.OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        void IFileDownloaderEventEmitters.OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        void IFileDownloaderEventEmitters.OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        void IFileDownloaderEventEmitters.OnDownloadCompleted(DownloadCompletedEventArgs ea) => _downloadCompleted?.Invoke(this, ea);
        void IFileDownloaderEventEmitters.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        void IFileDownloaderEventEmitters.OnFileDownloadProgressPercentageAndThroughputDataChanged(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileDownloadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);


        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFileDownloaderProxy
        internal class GenericNativeFileDownloaderCallbacksProxy : INativeFileDownloaderCallbacksProxy
        {
            public IFileDownloaderEventEmitters FileDownloader { get; set; }

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

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => FileDownloader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));

            public void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput)
                => FileDownloader?.OnFileDownloadProgressPercentageAndThroughputDataChanged(new FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
        }
    }
}