// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader, IFileUploaderEventEmitters
    {
        private readonly INativeFileUploaderProxy _nativeFileUploaderProxy;

        public string LastFatalErrorMessage => _nativeFileUploaderProxy?.LastFatalErrorMessage;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileUploaderProxy
        internal FileUploader(INativeFileUploaderProxy nativeFileUploaderProxy)
        {
            _nativeFileUploaderProxy = nativeFileUploaderProxy ?? throw new ArgumentNullException(nameof(nativeFileUploaderProxy));
            _nativeFileUploaderProxy.FileUploader = this; //vital
        }

        public EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
        {
            data = data ?? throw new ArgumentNullException(nameof(data));
            
            RemoteFilePathHelpers.ValidateRemoteFilePath(remoteFilePath); //                    order
            remoteFilePath = RemoteFilePathHelpers.SanitizeRemoteFilePath(remoteFilePath); //   order

            var verdict = _nativeFileUploaderProxy.BeginUpload(remoteFilePath, data);

            return verdict;
        }
        
        public void Cancel() => _nativeFileUploaderProxy?.Cancel();
        public void Disconnect() => _nativeFileUploaderProxy?.Disconnect();
        
        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<UploadCompletedEventArgs> _uploadCompleted;
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
        
        public event EventHandler<UploadCompletedEventArgs> UploadCompleted
        {
            add
            {
                _uploadCompleted -= value;
                _uploadCompleted += value;
            }
            remove => _uploadCompleted -= value;
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

        public async Task<IEnumerable<string>> UploadAsync(
            IDictionary<string, byte[]> remoteFilePathsAndTheirDataBytes,
            int sleepTimeBetweenRetriesInMs = 100,
            int timeoutPerUploadInMs = -1,
            int maxRetriesPerUpload = 10
        )
        {
            RemoteFilePathHelpers.ValidateRemoteFilePathsWithDataBytes(remoteFilePathsAndTheirDataBytes);
            var sanitizedRemoteFilePathsAndTheirDataBytes = RemoteFilePathHelpers.SanitizeRemoteFilePathsWithDataBytes(remoteFilePathsAndTheirDataBytes);

            var filesThatFailedToBeUploaded = new List<string>(2);

            foreach (var x in sanitizedRemoteFilePathsAndTheirDataBytes)
            {
                try
                {
                    await UploadAsync(
                        localData: x.Value,
                        remoteFilePath: x.Key,
                        maxRetriesCount: maxRetriesPerUpload,
                        timeoutForUploadInMs: timeoutPerUploadInMs,
                        sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
                    );
                }
                catch (UploadErroredOutException) //00
                {
                    filesThatFailedToBeUploaded.Add(x.Key);
                }
            }

            return filesThatFailedToBeUploaded;

            //00  we prefer to upload as many files as possible and report any failures collectively at the very end   we resorted to this
            //    tactic because failures are fairly common when uploading 50 files or more over to aed devices and we wanted to ensure
            //    that it would be as easy as possible to achieve the mass uploading just by using the default settings 
        }

        public async Task UploadAsync(
            byte[] localData,
            string remoteFilePath,
            int timeoutForUploadInMs = -1,
            int maxRetriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 1_000
        )
        {
            var isCancellationRequested = false;
            for (var retry = 0; !isCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSource<bool>(state: false);
                try
                {
                    Cancelled += UploadAsyncOnCancelled;
                    StateChanged += UploadAsyncOnStateChanged;
                    FatalErrorOccurred += UploadAsyncOnFatalErrorOccurred;

                    var verdict = BeginUpload(remoteFilePath, localData); //00 dont use task.run here for now
                    if (verdict != EFileUploaderVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    _ = timeoutForUploadInMs < 0
                        ? await taskCompletionSource.Task
                        : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutForUploadInMs); //order

                    break;
                }
                catch (TimeoutException ex)
                {
                    (this as IFileUploaderEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: EFileUploaderState.None, //better not use this.State here because the native call might fail
                        newState: EFileUploaderState.Error,
                        resource: remoteFilePath
                    ));

                    throw new UploadTimeoutException(remoteFilePath, timeoutForUploadInMs, ex);
                }
                catch (UploadErroredOutException ex) //errors with codes unknown(1) and in_value(3) happen all the time in android when multiuploading files
                {
                    if (ex is UploadErroredOutRemoteFolderNotFoundException) //no point to retry if the remote parent folder is not there
                        throw;

                    if (++retry > maxRetriesCount)
                        throw new UploadErroredOutException($"Failed to upload '{remoteFilePath}' after trying {maxRetriesCount + 1} time(s)", innerException: ex);

                    if (sleepTimeBetweenRetriesInMs > 0)
                    {
                        await Task.Delay(sleepTimeBetweenRetriesInMs);
                    }

                    continue;
                }
                catch (Exception ex) when (
                    !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                    && !(ex is TimeoutException)
                    && !(ex is IUploadRelatedException) //this accounts for both cancellations and upload errors
                )
                {
                    (this as IFileUploaderEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: EFileUploaderState.None,
                        newState: EFileUploaderState.Error,
                        resource: remoteFilePath
                    ));

                    // OnFatalErrorOccurred(); //better not   too much fuss
                    
                    throw new UploadErroredOutException(ex.Message, ex); //todo   better throw our own custom internal error exception here
                }
                finally
                {
                    Cancelled -= UploadAsyncOnCancelled;
                    StateChanged -= UploadAsyncOnStateChanged;
                    FatalErrorOccurred -= UploadAsyncOnFatalErrorOccurred;
                }

                void UploadAsyncOnCancelled(object sender, CancelledEventArgs ea)
                {
                    taskCompletionSource.TrySetException(new UploadCancelledException());
                }

                // ReSharper disable AccessToModifiedClosure
                void UploadAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
                {
                    switch (ea.NewState)
                    {
                        case EFileUploaderState.Complete:
                            taskCompletionSource.TrySetResult(true);
                            return;

                        case EFileUploaderState.Cancelling: //20
                            if (isCancellationRequested)
                                return;

                            isCancellationRequested = true;

                            Task.Run(async () =>
                            {
                                try
                                {
                                    await Task.Delay(5_000); //                                                     we first wait to allow the cancellation to occur normally
                                    (this as IFileUploaderEventEmitters).OnCancelled(new CancelledEventArgs()); //  but if it takes too long we give the killing blow manually
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
                throw new UploadCancelledException(); //20

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

        void IFileUploaderEventEmitters.OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        void IFileUploaderEventEmitters.OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        void IFileUploaderEventEmitters.OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        void IFileUploaderEventEmitters.OnUploadCompleted(UploadCompletedEventArgs ea) => _uploadCompleted?.Invoke(this, ea);
        void IFileUploaderEventEmitters.OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        void IFileUploaderEventEmitters.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        void IFileUploaderEventEmitters.OnFileUploadProgressPercentageAndThroughputDataChanged(FileUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _fileUploadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);
        
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFileUploaderProxy
        internal class GenericNativeFileUploaderCallbacksProxy : INativeFileUploaderCallbacksProxy
        {
            public IFileUploaderEventEmitters FileUploader { get; set; }

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

            public void UploadCompletedAdvertisement(string resource)
                => FileUploader?.OnUploadCompleted(new UploadCompletedEventArgs(resource));

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage)
                => FileUploader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(resource, errorMessage));

            public void FileUploadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput)
                => FileUploader?.OnFileUploadProgressPercentageAndThroughputDataChanged(new FileUploadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
        }
    }
}