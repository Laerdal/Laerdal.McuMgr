using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task SingleFileDownloadAsync_ShouldThrowDownloadTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedGreenButSlowNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath, timeoutForDownloadInMs: 100));

            // Assert
            await work.Should().ThrowExactlyAsync<DownloadTimeoutException>().WithTimeoutInMs((int)5.Seconds().TotalMilliseconds);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Error);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenButSlowNativeFileDownloaderProxySpy : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenButSlowNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null, int? windowCapacity = null, int? memoryAlignment = null)
            {
                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);

                    StateChangedAdvertisement(resource: remoteFilePath, oldState: EFileDownloaderState.Idle, newState: EFileDownloaderState.Downloading);

                    await Task.Delay(1_000);

                    StateChangedAdvertisement(resource: remoteFilePath, oldState: EFileDownloaderState.Downloading, newState: EFileDownloaderState.Complete); // order
                    DownloadCompletedAdvertisement(remoteFilePath, []); //                                                                             order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}