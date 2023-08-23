using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Events;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using Xunit;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STDEOE.GFEM.010", 0)]
        [InlineData("FDT.SFDA.STDEOE.GFEM.020", 1)]
        public async Task SingleFileDownloadAsync_ShouldThrowDownloadErroredOutException_GivenFatalErrorMidflight(string testcaseDescription, int maxRetriesCount)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy4(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                remoteFilePath: remoteFilePath,
                maxRetriesCount: maxRetriesCount
            ));

            // Assert
            await work.Should()
                .ThrowExactlyAsync<DownloadErroredOutException>()
                .WithMessage("*failed to download*")
                .WithTimeoutInMs((int)((maxRetriesCount + 1) * 3).Seconds().TotalMilliseconds);

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor.Should().NotRaise(nameof(fileDownloader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.DownloadCompleted));

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FatalErrorOccurred))
                .WithSender(fileDownloader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage == "fatal error occurred");
            
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

        private class MockedGreenNativeFileDownloaderProxySpy4 : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenNativeFileDownloaderProxySpy4(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _ = mockedFileData;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                var verdict = base.BeginDownload(remoteFilePath);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);

                    await Task.Delay(2_000);
                    
                    FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred");

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}