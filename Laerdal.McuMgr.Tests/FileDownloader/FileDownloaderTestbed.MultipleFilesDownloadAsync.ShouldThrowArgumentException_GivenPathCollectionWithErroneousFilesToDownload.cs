using FluentAssertions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.010", new[] { "/foo/bar.bin", "" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.020", new[] { "/foo/bar.bin", null })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.030", new[] { "/foo/bar.bin", "/ping\f/pong.bin" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.030", new[] { "/foo/bar.bin", "/ping\r/pong.bin" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.040", new[] { "/foo/bar.bin", "/ping\n/pong.bin" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.050", new[] { "/foo/bar.bin", "/ping\r\n/pong.bin" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.030", new[] { "/foo/bar.bin", "ping/pong.bin/" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.040", new[] { "/foo/bar.bin", "/ping/pong.bin/" })]
        [InlineData("FDT.MFDA.STAE.GPCWEFTD.050", new[] { "/foo/bar.bin", "  ping/pong.bin/  " })] //2nd path gets normalized to  "/ping/pong.bin/" which is invalid due to the trailing slash 
        public async Task MultipleFilesDownloadAsync_ShouldThrowArgumentException_GivenPathCollectionWithErroneousFilesToDownload(string testcaseNickname, IEnumerable<string> remoteFilePaths)
        {
            // Arrange
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy11(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(remoteFilePaths));

            // Assert
            await work.Should().ThrowExactlyAsync<ArgumentException>().WithTimeoutInMs(500);

            eventsMonitor.OccurredEvents.Should().HaveCount(0);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy11 : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenNativeFileDownloaderProxySpy11(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy) : base(downloaderCallbacksProxy)
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
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); //  order
                    DownloadCompletedAdvertisement(remoteFilePath, new byte[] { }); //                                              order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}