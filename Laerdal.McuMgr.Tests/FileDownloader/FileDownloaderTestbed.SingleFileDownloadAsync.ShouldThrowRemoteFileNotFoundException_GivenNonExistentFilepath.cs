using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STRFNFE.GNEF.010", "NO ENTRY (5)", 0)] //android
        [InlineData("FDT.SFDA.STRFNFE.GNEF.020", "NO ENTRY (5)", 1)] //android
        [InlineData("FDT.SFDA.STRFNFE.GNEF.030", "NO ENTRY (5)", 2)] //android
        [InlineData("FDT.SFDA.STRFNFE.GNEF.040", "NO_ENTRY (5)", 1)] //ios
        public async Task SingleFileDownloadAsync_ShouldThrowRemoteFileNotFoundException_GivenNonExistentFilepath(string testcaseNickname, string nativeErrorMessageForFileNotFound, int maxRetriesCount)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/non-existent/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedErroneousNativeFileDownloaderProxySpy2(
                mockedFileData: mockedFileData,
                downloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_(),
                nativeErrorMessageForFileNotFound: nativeErrorMessageForFileNotFound
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
                .ThrowExactlyAsync<DownloadErroredOutRemoteFileNotFoundException>()
                .WithTimeoutInMs((int)3.Seconds().TotalMilliseconds);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            eventsMonitor.OccurredEvents
                .Count(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should()
                .Be(1); //we just want to ensure that the method fails early and doesnt retry because there is no point to retry if the file doesnt exist

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FatalErrorOccurred))
                .WithSender(fileDownloader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage.ToUpperInvariant().Contains(nativeErrorMessageForFileNotFound.ToUpperInvariant()));

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

        private class MockedErroneousNativeFileDownloaderProxySpy2 : MockedNativeFileDownloaderProxySpy
        {
            private readonly string _nativeErrorMessageForFileNotFound;
            
            public MockedErroneousNativeFileDownloaderProxySpy2(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData, string nativeErrorMessageForFileNotFound) : base(downloaderCallbacksProxy)
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
                    
                    FatalErrorOccurredAdvertisement(_nativeErrorMessageForFileNotFound);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}