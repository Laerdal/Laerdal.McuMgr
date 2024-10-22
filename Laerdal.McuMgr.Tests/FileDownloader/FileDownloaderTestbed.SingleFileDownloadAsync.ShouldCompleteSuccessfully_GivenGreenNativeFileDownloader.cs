using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.SCS.GGNFD.010", "path/to/file.bin", 01, +100)] // this should be normalized to /path/to/file.bin
        [InlineData("FDT.SFDA.SCS.GGNFD.020", "/path/to/file.bin", 2, -100)] // negative sleep time should be interpreted as 0
        [InlineData("FDT.SFDA.SCS.GGNFD.030", "/path/to/file.bin", 2, +000)]
        [InlineData("FDT.SFDA.SCS.GGNFD.040", "/path/to/file.bin", 2, +100)]
        [InlineData("FDT.SFDA.SCS.GGNFD.050", "/path/to/file.bin", 3, -100)]
        [InlineData("FDT.SFDA.SCS.GGNFD.060", "/path/to/file.bin", 3, +000)]
        [InlineData("FDT.SFDA.SCS.GGNFD.070", "/path/to/file.bin", 3, +100)]
        public async Task SingleFileDownloadAsync_ShouldCompleteSuccessfully_GivenGreenNativeFileDownloader(string testcaseNickname, string remoteFilePath, int maxTriesCount, int sleepTimeBetweenRetriesInMs)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            var expectedRemoteFilepath = remoteFilePath.StartsWith('/')
                ? remoteFilePath
                : $"/{remoteFilePath}";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy(
                mockedFileData: mockedFileData,
                downloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_(),
                maxNumberOfTriesForSuccess: maxTriesCount
            );
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                maxTriesCount: maxTriesCount,
                remoteFilePath: remoteFilePath,
                sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
            ));

            // Assert
            await work.Should().CompleteWithinAsync(((maxTriesCount + 1) * 2).Seconds());

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();
            
            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should().HaveCount(maxTriesCount - 1); //one error for each try except the last one
            
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
            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null)
            {
                _tryCount++;

                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);
                    
                    await Task.Delay(20);
                    if (_tryCount < _maxNumberOfTriesForSuccess)
                    {
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error);
                        FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred", EMcuMgrErrorCode.Unknown, EFileOperationGroupReturnCode.Unset);
                        return;
                    }
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); //   order
                    DownloadCompletedAdvertisement(remoteFilePath, _mockedFileData); //                                              order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}