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

            public virtual EFileUploaderVerdict BeginUpload(
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
                BeginUploadCalled = true;

                return EFileUploaderVerdict.Success;
            }

            public virtual bool TryPause() => PauseCalled = true;
            public virtual bool TryResume() => ResumeCalled = true;
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
                => _uploaderCallbacksProxy.StateChangedAdvertisement( //raises the actual event
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    newState: newState,
                    oldState: oldState,
                    totalBytesToBeUploaded: totalBytesToBeUploaded
                );

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

            public void CleanupResourcesOfLastUpload()
            {
            }
        }
    }
}