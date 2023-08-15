using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Xunit;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderShould
    {
        [Theory]
        [InlineData("FDS.SCSODA.GGNFD.010", "path/to/file.bin")] // this should be normalized to /path/to/file.bin
        [InlineData("FDS.SCSODA.GGNFD.020", "/path/to/file.bin")]
        public async Task ShouldCompleteSuccessfullyOnDownloadAsync_GivenGreenNativeFileDownloader(string testcaseNickname, string remoteFilePath)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            var expectedRemoteFilepath = remoteFilePath.StartsWith("/")
                ? remoteFilePath
                : $"/{remoteFilePath}";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(new McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: remoteFilePath));

            // Assert
            await work.Should().CompleteWithinAsync(5.Seconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == expectedRemoteFilepath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == expectedRemoteFilepath && args.NewState == EFileDownloaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.DownloadCompleted))
                .WithSender(fileDownloader)
                .WithArgs<DownloadCompletedEventArgs>(args => args.Resource == expectedRemoteFilepath && args.Data.SequenceEqual(mockedFileData));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy : MockedNativeFileDownloaderProxySpy
        {
            private readonly byte[] _mockedFileData;

            public MockedGreenNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
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