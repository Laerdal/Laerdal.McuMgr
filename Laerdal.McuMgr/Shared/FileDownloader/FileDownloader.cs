// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Events;
using Laerdal.McuMgr.FileDownloader.Exceptions;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
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

        public async Task<Dictionary<string, byte[]>> DownloadAsync(
            IEnumerable<string> remoteFilePaths,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int timeoutPerDownloadInMs = -1,
            int maxRetriesPerDownload = 10
        )
        {
            var results = remoteFilePaths.ToDictionary(
                keySelector: x => x,
                elementSelector: x => (byte[]) null
            );

            foreach (var x in results)
            {
                try
                {
                    var data = await DownloadAsync(
                        x.Key,
                        maxRetriesCount: maxRetriesPerDownload,
                        timeoutForDownloadInMs: timeoutPerDownloadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
                    );
                    
                    results[x.Key] = data;
                }
                catch (DownloadErroredOutRemoteFileNotFoundException)
                {
                    continue;
                }
            }

            return results;
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
                    if (verdict != IFileDownloader.EFileDownloaderVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    result = timeoutForDownloadInMs < 0
                        ? await taskCompletionSource.Task
                        : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutForDownloadInMs);
                }
                catch (TimeoutException ex)
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: IFileDownloader.EFileDownloaderState.None, //better not use this.State here because the native call might fail
                        newState: IFileDownloader.EFileDownloaderState.Error
                    ));

                    throw new DownloadTimeoutException(remoteFilePath, timeoutForDownloadInMs, ex);
                }
                catch (DownloadErroredOutException ex)
                {
                    if (ex is DownloadErroredOutRemoteFileNotFoundException) //no point to retry if the remote file is not there
                        throw;
                    
                    if (++retry > maxRetriesCount)
                        throw new DownloadErroredOutException($"Failed to download '{remoteFilePath}' after trying {maxRetriesCount} times", innerException: ex);

                    if (sleepTimeBetweenRetriesInMs > 0)
                    {
                        await Task.Delay(sleepTimeBetweenRetriesInMs);
                    }
                }
                catch (Exception ex) when (
                    !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                    && !(ex is TimeoutException)
                    && !(ex is DownloadErroredOutException)
                )
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: IFileDownloader.EFileDownloaderState.None,
                        newState: IFileDownloader.EFileDownloaderState.Error
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

                continue;

                void DownloadAsyncOnCancelled(object sender, CancelledEventArgs ea)
                {
                    taskCompletionSource.TrySetException(new DownloadCancelledException());
                }

                void DownloadAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
                {
                    if (ea.NewState != IFileDownloader.EFileDownloaderState.Cancelling || isCancellationRequested)
                        return;

                    isCancellationRequested = true;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(5_000); //                                                 we first wait to allow the cancellation to occur normally
                            taskCompletionSource.TrySetException(new DownloadCancelledException()); //  but if it takes too long we give the killing blow manually
                        }
                        catch // (Exception ex)
                        {
                            // ignored
                        }
                    });
                    return;
                }

                void DownloadAsyncOnDownloadCompleted(object sender, DownloadCompletedEventArgs ea)
                {
                    taskCompletionSource.TrySetResult(ea.Data);
                }

                void DownloadAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
                {
                    var isAboutRemoteFileNotFound = ea.ErrorMessage?
                        .ToUpperInvariant()
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
            //    the upload cannot commence to begin with
        }
        
        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnDownloadCompleted(DownloadCompletedEventArgs ea) => _downloadCompleted?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        private void OnFileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(FileDownloadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileDownloadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);
    }
}