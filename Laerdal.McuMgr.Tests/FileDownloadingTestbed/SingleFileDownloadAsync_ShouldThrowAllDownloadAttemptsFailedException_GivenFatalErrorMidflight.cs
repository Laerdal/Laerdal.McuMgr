using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.STDEOE.GFEM.010", 1)]
        [InlineData("FDT.SFDA.STDEOE.GFEM.020", 2)]
        public async Task SingleFileDownloadAsync_ShouldThrowAllDownloadAttemptsFailedException_GivenFatalErrorMidflight(string testcaseDescription, int maxTriesCount)
        {
            // Arrange
            var allLogEas = new List<LogEmittedEventArgs>(8);
            var mockedFileData = new byte[] { 1, 2, 3 };
            const string remoteFilePath = "/path/to/file.bin";

            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy4(new GenericNativeFileDownloaderCallbacksProxy_(), mockedFileData);
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() =>
            {
                fileDownloader.LogEmitted += (object _, in LogEmittedEventArgs ea) => allLogEas.Add(ea);
                
                return fileDownloader.DownloadAsync(
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp.",
                    maxTriesCount: maxTriesCount,
                    remoteFilePath: remoteFilePath
                );
            });

            // Assert
            await work.Should()
                .ThrowWithinAsync<AllDownloadAttemptsFailedException>((maxTriesCount * 3).Seconds())
                .WithMessage("*failed to download*");

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor.Should().Raise(nameof(fileDownloader.FileDownloadStarted));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.Cancelled));
            eventsMonitor.Should().NotRaise(nameof(fileDownloader.FileDownloadCompleted));

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FatalErrorOccurred))
                .WithSender(fileDownloader)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage == "fatal error occurred");

            // eventsMonitor
            //     .OccurredEvents
            //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
            //     .SelectMany(x => x.Parameters)
            //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(

            allLogEas
                .Count(l => l is {Level: ELogLevel.Error} && l.Message.Contains("fatal error occurred", StringComparison.InvariantCulture))
                .Should()
                .BeGreaterThanOrEqualTo(1);
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.RemoteFilePath == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.RemoteFilePath == remoteFilePath && args.NewState == EFileDownloaderState.Error);
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy4 : MockedNativeFileDownloaderProxySpy
        {
            public MockedGreenNativeFileDownloaderProxySpy4(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy, byte[] mockedFileData) : base(downloaderCallbacksProxy)
            {
                _ = mockedFileData;
            }

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null)
            {
                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading, 1_024, null);

                    await Task.Delay(2_000);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error, 0, null);
                    FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred", EGlobalErrorCode.Generic);
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native downloader
            }
        }
    }
}