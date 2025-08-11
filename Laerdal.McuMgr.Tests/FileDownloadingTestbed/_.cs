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

            public bool CancelCalled { get; private set; }
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

            public virtual EFileDownloaderVerdict BeginDownload(
                string remoteFilePath,
                int? initialMtuSize = null
            )
            {
                BeginDownloadCalled = true;

                return EFileDownloaderVerdict.Success;
            }

            public virtual void Cancel()
            {
                CancelCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void CancelledAdvertisement() 
                => _downloaderCallbacksProxy.CancelledAdvertisement(); //raises the actual event
            
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => _downloaderCallbacksProxy.LogMessageAdvertisement(message, category, level, resource); //raises the actual event

            public void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState)
                => _downloaderCallbacksProxy.StateChangedAdvertisement(resourceId: resource, newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _downloaderCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event
            
            public void DownloadCompletedAdvertisement(string resource, byte[] data)
                => _downloaderCallbacksProxy.DownloadCompletedAdvertisement(resource, data); //raises the actual event
            
            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EGlobalErrorCode globalErrorCode) 
                => _downloaderCallbacksProxy.FatalErrorOccurredAdvertisement(resource, errorMessage, globalErrorCode); //raises the actual event
            
            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, int progressPercentage, float currentThroughputInKbps, float totalAverageThroughputInKbps)
                => _downloaderCallbacksProxy.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, progressPercentage, currentThroughputInKbps, totalAverageThroughputInKbps); //raises the actual event
            
            public bool TrySetContext(object context) => throw new NotImplementedException();
            public bool TrySetBluetoothDevice(object bluetoothDevice) => throw new NotImplementedException();
            public bool TryInvalidateCachedTransport() => throw new NotImplementedException();

            public void Dispose()
            {
            }
        }
    }
}