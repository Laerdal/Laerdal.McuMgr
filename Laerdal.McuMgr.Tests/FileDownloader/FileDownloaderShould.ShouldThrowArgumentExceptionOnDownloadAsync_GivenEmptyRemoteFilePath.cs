using System;
using System.Threading.Tasks;
using FluentAssertions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderShould
    {
        [Fact]
        public async Task ShouldThrowArgumentExceptionOnDownloadAsync_GivenEmptyRemoteFilePath()
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy2(new McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath));

            // Assert
            await work.Should().ThrowExactlyAsync<ArgumentException>().WithTimeoutInMs(50);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFileDownloaderProxySpy2 : MockedNativeFileDownloaderProxySpy
        {
            private readonly byte[] _mockedFileData;
            
            public MockedGreenNativeFileDownloaderProxySpy2(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    Task.Delay(20).GetAwaiter().GetResult();
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}