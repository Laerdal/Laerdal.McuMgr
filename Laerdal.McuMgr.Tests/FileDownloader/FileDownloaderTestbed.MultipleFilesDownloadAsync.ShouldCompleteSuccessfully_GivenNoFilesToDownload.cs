using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDS.MFDA.SCS.GGNFD.010", null)] //               this should be normalized to /path/to/file.bin
        [InlineData("FDS.MFDA.SCS.GGNFD.020", new string[] { })] //   negative sleep time should be interpreted as 0
        public async Task MultipleFilesDownloadAsync_ShouldCompleteSuccessfully_GivenNoFilesToDownload(string testcaseNickname, IEnumerable<string> remoteFilePaths)
        {
            // Arrange
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy5(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(remoteFilePaths));

            // Assert
            var results = (await work.Should().CompleteWithinAsync(10.Milliseconds())).Which;

            results.Should().BeEmpty();
            eventsMonitor.OccurredEvents.Should().HaveCount(0);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy5 : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenNativeFileDownloaderProxySpy5(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy) : base(downloaderCallbacksProxy)
            {
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);
                    
                    await Task.Delay(20);
                    DownloadCompletedAdvertisement(remoteFilePath, new byte[] { });
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}