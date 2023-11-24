using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STUEOE.GRNEM.010", "", 1)] //    we want to ensure that our error sniffing logic will 
        [InlineData("FDT.SFDA.STUEOE.GRNEM.020", null, 1)] //  not be error out itself by rogue native error messages
        public async Task SingleFileDownloadAsync_ShouldThrowAllDownloadAttemptsFailedException_GivenRogueNativeErrorMessage(string testcaseNickname, string nativeRogueErrorMessage, int maxRetriesCount)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/non-existent/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedErroneousNativeFileDownloaderProxySpy13(
                mockedFileData: mockedFileData,
                downloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_(),
                nativeErrorMessageForFileNotFound: nativeRogueErrorMessage
            );
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                remoteFilePath: remoteFilePath,
                maxRetriesCount: maxRetriesCount, //doesnt really matter   we just want to ensure that the method fails early and doesnt retry
                sleepTimeBetweenRetriesInMs: 10
            ));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<AllDownloadAttemptsFailedException>()
                .WithTimeoutInMs((int)3.Seconds().TotalMilliseconds);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            eventsMonitor.OccurredEvents
                .Count(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should()
                .Be(1 + maxRetriesCount);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FatalErrorOccurred))
                .WithSender(fileDownloader);

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

        private class MockedErroneousNativeFileDownloaderProxySpy13 : MockedNativeFileDownloaderProxySpy
        {
            private readonly string _nativeErrorMessageForFileNotFound;
            
            public MockedErroneousNativeFileDownloaderProxySpy13(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData, string nativeErrorMessageForFileNotFound) : base(downloaderCallbacksProxy)
            {
                _ = mockedFileData;
                _nativeErrorMessageForFileNotFound = nativeErrorMessageForFileNotFound;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    await Task.Delay(100);
                    
                    FatalErrorOccurredAdvertisement(remoteFilePath, _nativeErrorMessageForFileNotFound);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}