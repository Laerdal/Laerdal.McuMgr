using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        private class MockedNativeFileDownloaderProxySpy : INativeFileDownloaderProxy //template class for all spies
        {
            private readonly INativeFileDownloaderCallbacksProxy _downloaderCallbacksProxy;

            public string CurrentRemoteFilePath { get; private set; }
            public EFileDownloaderState CurrentState { get; protected set; }
            
            public bool PauseCalled { get; private set; }
            public bool CancelCalled { get; private set; }
            public bool ResumeCalled { get; private set; }
            public bool DisconnectCalled { get; private set; }
            public bool BeginDownloadCalled { get; private set; }

            public string LastFatalErrorMessage => "";

            public IFileDownloaderEventEmittable FileDownloader //keep this to conform to the interface
            {
                get => _downloaderCallbacksProxy!.FileDownloader;
                set => _downloaderCallbacksProxy!.FileDownloader = value;
            }

            protected MockedNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy)
            {
                _downloaderCallbacksProxy = downloaderCallbacksProxy;
            }

            public virtual EFileDownloaderVerdict NativeBeginDownload(
                string remoteFilePath,
                int? initialMtuSize = null
            )
            {
                BeginDownloadCalled = true;
                CurrentRemoteFilePath = remoteFilePath;

                if (!IsCold()) //order   emulating the native-layer
                {
                    StateChangedAdvertisement( //emulating the native-layer
                        remoteFilePath: remoteFilePath,
                        oldState: CurrentState,
                        newState: EFileDownloaderState.Error,
                        completeDownloadedData: null,
                        totalBytesToBeDownloaded: 0
                    );
                    return EFileDownloaderVerdict.FailedDownloadAlreadyInProgress;
                }

                return EFileDownloaderVerdict.Success;
            }
            
            private bool IsCold()
            {
                return CurrentState == EFileDownloaderState.None //        this is what the native-layer does
                       || CurrentState == EFileDownloaderState.Error //    and we must keep this mock updated
                       || CurrentState == EFileDownloaderState.Complete // to reflect this fact
                       || CurrentState == EFileDownloaderState.Cancelled;
            }

            protected readonly ManualResetEventSlim PauseDownloadGuard = new(initialState: true);
            public virtual bool TryPause()
            {
                PauseCalled = true; //keep first
                
                if (CurrentState == EFileDownloaderState.Paused) //order
                    return true; //already paused
                
                if (CurrentState != EFileDownloaderState.Downloading)
                    return false; //can only pause when we are actually uploading something

                PauseDownloadGuard.Reset(); //capture the lock

                StateChangedAdvertisement(remoteFilePath: CurrentRemoteFilePath, oldState: CurrentState, newState: EFileDownloaderState.Paused, totalBytesToBeDownloaded: 0, completeDownloadedData: null);
                BusyStateChangedAdvertisement(busyNotIdle: false);

                return true;
            }

            public virtual bool TryResume()
            {
                ResumeCalled = true; //order  keep first
                
                if (CurrentState == EFileDownloaderState.Resuming) //order
                    return true; //already resuming
                
                if (CurrentState != EFileDownloaderState.Paused)
                    return false; //not paused, cannot resume

                StateChangedAdvertisement(oldState: EFileDownloaderState.Paused, newState: EFileDownloaderState.Resuming, remoteFilePath: CurrentRemoteFilePath, totalBytesToBeDownloaded: 0, completeDownloadedData: null);
                BusyStateChangedAdvertisement(busyNotIdle: true);
                
                _ = Task.Run(async () => // under normal circumstances the native implementation will bubble-up the following callbacks
                {
                    await Task.Delay(5); // simulate the ble-layer lag involved in resuming the upload
                    StateChangedAdvertisement(oldState: EFileDownloaderState.Resuming, newState: EFileDownloaderState.Downloading, remoteFilePath: CurrentRemoteFilePath, totalBytesToBeDownloaded: 0, completeDownloadedData: null);

                    PauseDownloadGuard.Set(); //release the lock so that the upload can continue
                });

                return true;
            }
            
            
            public virtual bool TryCancel(string reason = "") => CancelCalled = true;
            public virtual bool TryDisconnect() => DisconnectCalled = true;

            public void CancelledAdvertisement(string reason)
                => _downloaderCallbacksProxy.CancelledAdvertisement(reason);
            
            public void CancellingAdvertisement(string reason)
                => _downloaderCallbacksProxy.CancellingAdvertisement(reason);

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => _downloaderCallbacksProxy.LogMessageAdvertisement(message, category, level, resource); //raises the actual event

            public void StateChangedAdvertisement(string remoteFilePath, EFileDownloaderState oldState, EFileDownloaderState newState, long totalBytesToBeDownloaded, byte[] completeDownloadedData)
            {
                CurrentState = newState;
                
                _downloaderCallbacksProxy.StateChangedAdvertisement( //raises the actual event
                    newState: newState,
                    oldState: oldState,
                    remoteFilePath: remoteFilePath,
                    completeDownloadedData: completeDownloadedData,
                    totalBytesToBeDownloaded: totalBytesToBeDownloaded
                );
            }

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _downloaderCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EGlobalErrorCode globalErrorCode) 
                => _downloaderCallbacksProxy.FatalErrorOccurredAdvertisement(resource, errorMessage, globalErrorCode); //raises the actual event

            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => _downloaderCallbacksProxy.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, progressPercentage, currentThroughputInKBps, totalAverageThroughputInKBps); //raises the actual event
            
            public bool TrySetContext(object context) => throw new NotImplementedException();
            public bool TrySetBluetoothDevice(object bluetoothDevice) => throw new NotImplementedException();
            public bool TryInvalidateCachedInfrastructure() => throw new NotImplementedException();

            public void Dispose()
            {
            }
        }
    }
}