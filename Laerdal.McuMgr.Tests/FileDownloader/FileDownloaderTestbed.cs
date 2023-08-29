using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
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

            public virtual EFileDownloaderVerdict BeginDownload(string remoteFilePath)
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
                => _downloaderCallbacksProxy.StateChangedAdvertisement(resource: resource, newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _downloaderCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event
            
            public void DownloadCompletedAdvertisement(string resource, byte[] data)
                => _downloaderCallbacksProxy.DownloadCompletedAdvertisement(resource, data); //raises the actual event

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage)
                => _downloaderCallbacksProxy.FatalErrorOccurredAdvertisement(resource, errorMessage); //raises the actual event
            
            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
                => _downloaderCallbacksProxy.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput); //raises the actual event
        }
    }
}