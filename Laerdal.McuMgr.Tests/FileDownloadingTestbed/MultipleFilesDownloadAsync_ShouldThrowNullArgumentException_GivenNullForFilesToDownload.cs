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
        [Fact]
        public async Task MultipleFilesDownloadAsync_ShouldThrowNullArgumentException_GivenNullForFilesToDownload()
        {
            // Arrange
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy10(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task<IDictionary<string, byte[]>>>(async () => await fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                remoteFilePaths: null
            ));

            // Assert
            await work.Should().ThrowWithinAsync<ArgumentNullException>(500.Milliseconds());

            eventsMonitor.OccurredEvents.Should().HaveCount(0);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeFalse();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy10 : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenNativeFileDownloaderProxySpy10(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy) : base(downloaderCallbacksProxy)
            {
            }

            public override EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null)
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