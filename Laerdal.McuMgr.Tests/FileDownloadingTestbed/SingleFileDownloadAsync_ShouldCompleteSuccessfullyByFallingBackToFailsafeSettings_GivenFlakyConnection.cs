using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Events;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Theory]
        [InlineData("FDT.SFDA.SCSBFBTFS.GFBC.010", "/path/to/file.bin", 2)]
        [InlineData("FDT.SFDA.SCSBFBTFS.GFBC.020", "/path/to/file.bin", 3)]
        [InlineData("FDT.SFDA.SCSBFBTFS.GFBC.030", "/path/to/file.bin", 5)]
        public async Task SingleFileDownloadAsync_ShouldCompleteSuccessfullyByFallingBackToFailsafeSettings_GivenFlakyBluetoothConnection(string testcaseNickname, string remoteFilePath, int maxTriesCount)
        {
            // Arrange
            var allLogEas = new List<LogEmittedEventArgs>(8);
            var expectedData = (byte[]) [1, 2, 3];
            
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy120(
                expectedData: expectedData,
                maxTriesCount: maxTriesCount,
                uploaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_()
            );
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
            await work.Should().CompleteWithinAsync((maxTriesCount * 400).Seconds());
            
            mockedNativeFileDownloaderProxy.BugDetected.Should().BeNull();
            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor
                .OccurredEvents
                .Where(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should().HaveCount(maxTriesCount - 1); //one error for each try except the last one
            
            eventsMonitor
                .Should()
                .Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            // eventsMonitor
            //     .OccurredEvents
            //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
            //     .SelectMany(x => x.Parameters)
            //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(

            allLogEas
                .Count(l => l is {Level: ELogLevel.Warning} && l.Message.Contains("[FD.DA.010]", StringComparison.InvariantCulture))
                .Should()
                .BeGreaterOrEqualTo(1);
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FileDownloadStarted))
                .WithSender(fileDownloader)
                .WithArgs<FileDownloadStartedEventArgs>(args => args.ResourceId == remoteFilePath);
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.FileDownloadCompleted))
                .WithSender(fileDownloader)
                .WithArgs<FileDownloadCompletedEventArgs>(args => args.ResourceId == remoteFilePath);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy120 : MockedNativeFileDownloaderProxySpy
        {
            private readonly int _maxTriesCount;
            private readonly byte[] _expectedData;

            public string BugDetected { get; private set; }

            public MockedGreenNativeFileDownloaderProxySpy120(byte[] expectedData, INativeFileDownloaderCallbacksProxy uploaderCallbacksProxy, int maxTriesCount) : base(uploaderCallbacksProxy)
            {
                _expectedData = expectedData;
                _maxTriesCount = maxTriesCount;
            }

            private int _tryCounter;
            public override EFileDownloaderVerdict BeginDownload(
                string remoteFilePath,
                int? initialMtuSize = null //  android only
            )
            {
                _tryCounter++;

                var verdict = base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize //   android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Idle, EFileDownloaderState.Downloading);
                    FileDownloadStartedAdvertisement(remoteFilePath, 1_024);

                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 00, 00, 00);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 10, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 20, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 30, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 40, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 50, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 60, 10, 10);

                    if (_tryCounter == _maxTriesCount && initialMtuSize == null)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(initialMtuSize)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error); //  order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EGlobalErrorCode.Generic); //                   order
                        return;
                    }

                    if (_tryCounter < _maxTriesCount)
                    {
                        await Task.Delay(20);
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error); //   order
                        FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred", EGlobalErrorCode.Generic); //         order
                        return;
                    }

                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 70, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 80, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 90, 10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePath, 100, 10, 10);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); // order
                    FileDownloadCompletedAdvertisement(remoteFilePath, _expectedData); //                                              order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}