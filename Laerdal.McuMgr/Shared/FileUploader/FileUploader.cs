// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Events;
using Laerdal.McuMgr.FileUploader.Exceptions;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
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

        public event EventHandler<FileUploadProgressPercentageAndDataThroughputChangedEventArgs> FileUploadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _fileUploadProgressPercentageAndDataThroughputChanged -= value;
                _fileUploadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _fileUploadProgressPercentageAndDataThroughputChanged -= value;
        }

        public async Task UploadAsync(
            IDictionary<string, byte[]> remoteFilePathsAndTheirDataBytes,
            int sleepTimeBetweenRetriesInMs = 1_000,
            int timeoutPerUploadInMs = -1,
            int maxRetriesPerUpload = 10
        )
        {
            if (remoteFilePathsAndTheirDataBytes == null)
                throw new ArgumentNullException(nameof(remoteFilePathsAndTheirDataBytes));

            var filesThatDidntGetUploadedYet = new HashSet<string>(remoteFilePathsAndTheirDataBytes.Select(x => x.Key));

            foreach (var x in remoteFilePathsAndTheirDataBytes)
            {
                if (!filesThatDidntGetUploadedYet.Contains(x.Key))
                    continue;

                try
                {
                    await UploadAsync(
                        localData: x.Value,
                        remoteFilePath: x.Key,
                        maxRetriesPerUpload: maxRetriesPerUpload,
                        timeoutForUploadInMs: timeoutPerUploadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
                    );

                    filesThatDidntGetUploadedYet.Remove(x.Key);
                }
                catch (UploadErroredOutException) //00
                {
                }
            }

            if (filesThatDidntGetUploadedYet.Any())
                throw new UploadErroredOutException(
                    $"The following files failed to be uploaded:{Environment.NewLine}{Environment.NewLine}" +
                    $"{string.Join(Environment.NewLine, filesThatDidntGetUploadedYet)}"
                );

            //00  we prefer to upload as many files as possible and report any failures collectively at the very end   we resorted to this
            //    tactic because failures are fairly common when uploading 50 files or more over to aed devices and we wanted to ensure
            //    that it would be as easy as possible to achieve the mass uploading just by using the default settings 
        }

        public async Task UploadAsync(
            byte[] localData,
            string remoteFilePath,
            int timeoutForUploadInMs = -1,
            int maxRetriesPerUpload = 10,
            int sleepTimeBetweenRetriesInMs = 1_000
        )
        {
            var isCancellationRequested = false;
            for (var retry = 0; !isCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSource<bool>(state: false);
                try
                {
                    StateChanged += UploadAsyncOnStateChanged;
                    FatalErrorOccurred += UploadAsyncOnFatalErrorOccurred;

                    var verdict = BeginUpload(remoteFilePath, localData); //00 dont use task.run here for now
                    if (verdict != IFileUploader.EFileUploaderVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    _ = timeoutForUploadInMs < 0
                        ? await taskCompletionSource.Task
                        : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutForUploadInMs); //order

                    break;
                }
                catch (TimeoutException ex)
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: IFileUploader.EFileUploaderState.None, //better not use this.State here because the native call might fail
                        newState: IFileUploader.EFileUploaderState.Error,
                        remoteFilePath: remoteFilePath
                    ));

                    throw new UploadTimeoutException(remoteFilePath, timeoutForUploadInMs, ex);
                }
                catch (UploadErroredOutException ex) //errors with codes unknown(1) and in_value(3) happen all the time in android when multiuploading files
                {
                    if (++retry > maxRetriesPerUpload)
                        throw new UploadErroredOutException($"Failed to upload '{remoteFilePath}' after trying {maxRetriesPerUpload} times", innerException: ex);

                    if (sleepTimeBetweenRetriesInMs > 0)
                    {
                        await Task.Delay(sleepTimeBetweenRetriesInMs);
                    }
                }
                catch (Exception ex) when (
                    !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                    && !(ex is TimeoutException)
                    && !(ex is UploadErroredOutException)
                )
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: IFileUploader.EFileUploaderState.None,
                        newState: IFileUploader.EFileUploaderState.Error,
                        remoteFilePath: remoteFilePath
                    ));

                    // OnFatalErrorOccurred(); //better not   too much fuss
                    
                    throw new UploadErroredOutException(ex.Message, ex); //todo   better throw our own custom internal error exception here
                }
                finally
                {
                    StateChanged -= UploadAsyncOnStateChanged;
                    FatalErrorOccurred -= UploadAsyncOnFatalErrorOccurred;
                }

                // ReSharper disable AccessToModifiedClosure
                void UploadAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
                {
                    switch (ea.NewState)
                    {
                        case IFileUploader.EFileUploaderState.Complete:
                            taskCompletionSource.TrySetResult(true);
                            return;

                        case IFileUploader.EFileUploaderState.Cancelled:
                            taskCompletionSource.TrySetException(new UploadCancelledException());
                            return;

                        case IFileUploader.EFileUploaderState.Cancelling: //20
                            if (isCancellationRequested)
                                return;

                            isCancellationRequested = true;

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await Task.Delay(5_000); //                                               we first wait to allow the cancellation to occur normally
                                    taskCompletionSource.TrySetException(new UploadCancelledException()); //  but if it takes too long we give the killing blow manually
                                }
                                catch // (Exception ex)
                                {
                                    // ignored
                                }
                            });
                            return;
                    }
                }

                void UploadAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
                {
                    var isAboutFolderNotExisting = ea.ErrorMessage?.ToUpperInvariant().Contains("UNKNOWN (1)") ?? false;
                    if (isAboutFolderNotExisting)
                    {
                        taskCompletionSource.TrySetException(new UploadErroredOutRemoteFolderNotFoundException(Path.GetDirectoryName(remoteFilePath))); //specific case
                        return;
                    }

                    taskCompletionSource.TrySetException(new UploadErroredOutException(ea.ErrorMessage)); //generic
                }
                // ReSharper restore AccessToModifiedClosure
            }
            
            if (isCancellationRequested) //vital
                throw new UploadCancelledException(); //10

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
        }

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        private void OnFileUploadProgressPercentageAndThroughputDataChangedAdvertisement(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileUploadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);
    }
}