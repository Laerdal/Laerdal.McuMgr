using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        private class BaseMockedNativeFileUploaderProxySpy : INativeFileUploaderProxy //template class for all spies
        {
            private readonly INativeFileUploaderCallbacksProxy _uploaderCallbacksProxy;

            public string CurrentResourceId  { get; private set; }
            public string CurrentRemoteFilePath  { get; private set; }
            public EFileUploaderState CurrentState { get; protected set; }

            public bool PauseCalled { get; private set; }
            public bool ResumeCalled { get; private set; }
            public bool CancelCalled { get; private set; }
            public bool DisconnectCalled { get; private set; }
            public bool BeginUploadCalled { get; private set; }
            public string CancellationReason { get; private set; }

            public string LastFatalErrorMessage => "";

            public IFileUploaderEventEmittable FileUploader //keep this to conform to the interface
            {
                get => _uploaderCallbacksProxy!.FileUploader;
                set => _uploaderCallbacksProxy!.FileUploader = value;
            }

            protected BaseMockedNativeFileUploaderProxySpy(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy)
            {
                _uploaderCallbacksProxy = uploaderCallbacksProxy;
            }

            public virtual EFileUploaderVerdict NativeBeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                int? initialMtuSize = null,
                int? pipelineDepth = null, //   ios
                int? byteAlignment = null, //   ios
                int? windowCapacity = null, //  android
                int? memoryAlignment = null //  android
            )
            {
                BeginUploadCalled = true; //order
                CurrentResourceId = resourceId;
                CurrentRemoteFilePath = remoteFilePath;
                
                if (!IsCold()) //order   emulating the native-layer
                {
                    StateChangedAdvertisement( //emulating the native-layer
                        resourceId: resourceId,
                        remoteFilePath: remoteFilePath,
                        oldState: CurrentState,
                        newState: EFileUploaderState.Error,
                        totalBytesToBeUploaded: 0
                    );
                    return EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress;
                }
                
                return EFileUploaderVerdict.Success;
            }
            
            private bool IsCold()
            {
                return CurrentState == EFileUploaderState.None //        this is what the native-layer does
                       || CurrentState == EFileUploaderState.Error //    and we must keep this mock updated
                       || CurrentState == EFileUploaderState.Complete // to reflect this fact
                       || CurrentState == EFileUploaderState.Cancelled;
            }

            protected readonly ManualResetEventSlim PauseGuard = new(initialState: true);
            public virtual bool TryPause()
            {
                PauseCalled = true; //keep first
                
                if (CurrentState == EFileUploaderState.Paused) //order
                    return true; //already paused
                
                if (CurrentState != EFileUploaderState.Uploading)
                    return false; //can only pause when we are actually uploading something

                PauseGuard.Reset(); //capture the lock

                StateChangedAdvertisement(resourceId: CurrentResourceId, remoteFilePath: CurrentRemoteFilePath, oldState: CurrentState, newState: EFileUploaderState.Paused, totalBytesToBeUploaded: 0);
                BusyStateChangedAdvertisement(busyNotIdle: false);

                return true;
            }

            public virtual bool TryResume()
            {
                ResumeCalled = true; //order  keep first
                
                if (CurrentState == EFileUploaderState.Resuming) //order
                    return true; //already resuming
                
                if (CurrentState != EFileUploaderState.Paused)
                    return false; //not paused, cannot resume

                StateChangedAdvertisement(oldState: EFileUploaderState.Paused, newState: EFileUploaderState.Resuming, resourceId: CurrentResourceId, remoteFilePath: CurrentRemoteFilePath, totalBytesToBeUploaded: 0);
                BusyStateChangedAdvertisement(busyNotIdle: true);
                
                _ = Task.Run(async () => // under normal circumstances the native implementation will bubble-up the following callbacks
                {
                    await Task.Delay(5); // simulate the ble-layer lag involved in resuming the upload
                    StateChangedAdvertisement(oldState: EFileUploaderState.Resuming, newState: EFileUploaderState.Uploading, resourceId: CurrentResourceId, remoteFilePath: CurrentRemoteFilePath, totalBytesToBeUploaded: 0);

                    PauseGuard.Set(); //release the lock so that the upload can continue
                });

                return true;
            }

            public virtual bool TryCancel(string reason = "")
            {
                CancellationReason = reason;
                return CancelCalled = true;
            }

            public virtual bool TryDisconnect() => DisconnectCalled = true;

            public void CancellingAdvertisement(string reason = "")
                => _uploaderCallbacksProxy.CancellingAdvertisement(reason); //raises the actual event

            public void CancelledAdvertisement(string reason = "")
                => _uploaderCallbacksProxy.CancelledAdvertisement(reason); //raises the actual event
            
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => _uploaderCallbacksProxy.LogMessageAdvertisement(message, category, level, resource); //raises the actual event

            public void StateChangedAdvertisement(string resourceId, string remoteFilePath, EFileUploaderState oldState, EFileUploaderState newState, long totalBytesToBeUploaded)
            {
                CurrentState = newState;
                
                _uploaderCallbacksProxy.StateChangedAdvertisement( //raises the actual event
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    newState: newState,
                    oldState: oldState,
                    totalBytesToBeUploaded: totalBytesToBeUploaded
                );
            }

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _uploaderCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event

            public void FatalErrorOccurredAdvertisement(
                string resourceId,
                string remoteFilePath,
                string errorMessage,
                EGlobalErrorCode globalErrorCode
            ) => _uploaderCallbacksProxy.FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, errorMessage, globalErrorCode); //raises the actual event
            
            public void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, string remoteFilePath, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => _uploaderCallbacksProxy.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, progressPercentage, currentThroughputInKBps, totalAverageThroughputInKBps); //raises the actual event
            
            public bool TrySetContext(object context) => throw new NotImplementedException();
            public bool TrySetBluetoothDevice(object bluetoothDevice) => throw new NotImplementedException();
            public bool TryInvalidateCachedInfrastructure() => throw new NotImplementedException();

            public void Dispose()
            {
            }

            public void TryCleanupResourcesOfLastUpload()
            {
            }
        }
    }
}