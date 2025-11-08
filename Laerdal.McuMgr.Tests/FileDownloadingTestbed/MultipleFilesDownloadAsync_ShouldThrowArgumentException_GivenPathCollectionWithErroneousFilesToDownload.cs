using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
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
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                remoteFilePaths: remoteFilePaths
            ));

            // Assert
            await work.Should().ThrowWithinAsync<ArgumentException>(500.Milliseconds());

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

            public override EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, ELogLevel? minimumLogLevel = null, int? initialMtuSize = null)
            {
                base.NativeBeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );
                
                StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.None, EFileDownloaderState.None, totalBytesToBeDownloaded: 0, null);
                StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.None, EFileDownloaderState.Idle, totalBytesToBeDownloaded: 0, null);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading, totalBytesToBeDownloaded: 1_024, null);
                    
                    await Task.Delay(20);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete, totalBytesToBeDownloaded: 1_024, []);
                });

                return EFileDownloaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}