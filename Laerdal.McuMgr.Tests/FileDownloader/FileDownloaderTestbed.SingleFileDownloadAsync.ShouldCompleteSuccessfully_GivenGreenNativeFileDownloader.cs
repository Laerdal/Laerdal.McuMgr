using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDS.SFDA.SCS.GGNFD.010", "path/to/file.bin", 00, +100)] // this should be normalized to /path/to/file.bin
        [InlineData("FDS.SFDA.SCS.GGNFD.020", "/path/to/file.bin", 1, -100)] // negative sleep time should be interpreted as 0
        [InlineData("FDS.SFDA.SCS.GGNFD.030", "/path/to/file.bin", 1, +000)]
        [InlineData("FDS.SFDA.SCS.GGNFD.040", "/path/to/file.bin", 1, +100)]
        [InlineData("FDS.SFDA.SCS.GGNFD.050", "/path/to/file.bin", 2, -100)]
        [InlineData("FDS.SFDA.SCS.GGNFD.060", "/path/to/file.bin", 2, +000)]
        [InlineData("FDS.SFDA.SCS.GGNFD.070", "/path/to/file.bin", 2, +100)]
        public async Task SingleFileDownloadAsync_ShouldCompleteSuccessfully_GivenGreenNativeFileDownloader(string testcaseNickname, string remoteFilePath, int maxRetriesCount, int sleepTimeBetweenRetriesInMs)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            var expectedRemoteFilepath = remoteFilePath.StartsWith("/")
                ? remoteFilePath
                : $"/{remoteFilePath}";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(
                mockedFileData: mockedFileData,
                downloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_(),
                maxNumberOfTriesForSuccess: maxRetriesCount + 1
            );
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                remoteFilePath: remoteFilePath,
                maxRetriesCount: maxRetriesCount,
                sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
            ));

            // Assert
            await work.Should().CompleteWithinAsync(((maxRetriesCount + 1) * 2).Seconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();
            
            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should().HaveCount(maxRetriesCount); //one error for each try except the last one
            
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
            private readonly int _maxNumberOfTriesForSuccess;

            public MockedGreenNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData, int maxNumberOfTriesForSuccess) : base(downloaderCallbacksProxy)
            {
                _mockedFileData = mockedFileData;
                _maxNumberOfTriesForSuccess = maxNumberOfTriesForSuccess;
            }

            private int _tryCount;
            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                _tryCount++;
                
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);
                    
                    await Task.Delay(20);
                    if (_tryCount < _maxNumberOfTriesForSuccess)
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error);
                        FatalErrorOccurredAdvertisement("fatal error occurred");
                        return;
                    }
                    
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}