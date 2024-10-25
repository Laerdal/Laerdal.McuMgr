using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
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
        [InlineData("FDT.SFDA.SCSBFBTFS.GFBC.010", "/path/to/file.bin", 2)]
        // [InlineData("FDT.SFDA.SCSBFBTFS.GFBC.020", "/path/to/file.bin", 3)]
        // [InlineData("FDT.SFDA.SCSBFBTFS.GFBC.030", "/path/to/file.bin", 5)]
        public async Task SingleFileDownloadAsync_ShouldCompleteSuccessfullyByFallingBackToFailsafeSettings_GivenFlakyBluetoothConnection(string testcaseNickname, string remoteFilePath, int maxTriesCount)
        {
            // Arrange
            var expectedData = (byte[]) [1, 2, 3];
            
            var mockedNativeFileDownloaderProxy = new MockedGreenNativeFileDownloaderProxySpy120(
                expectedData: expectedData,
                maxTriesCount: maxTriesCount,
                uploaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy_()
            );
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            using var eventsMonitor = fileDownloader.Monitor();

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                maxTriesCount: maxTriesCount,
                remoteFilePath: remoteFilePath
            ));

            // Assert
            await work.Should().CompleteWithinAsync((maxTriesCount * 200).Seconds());
            
            mockedNativeFileDownloaderProxy.BugDetected.Should().BeNull();
            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileDownloader.FatalErrorOccurred))
                .Should().HaveCount(maxTriesCount - 1); //one error for each try except the last one
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Downloading);

            eventsMonitor
                .OccurredEvents
                .Where(x => x.EventName == nameof(fileDownloader.LogEmitted))
                .SelectMany(x => x.Parameters)
                .OfType<LogEmittedEventArgs>()
                .Count(l => l is { Level: ELogLevel.Warning } && l.Message.Contains("[FD.DA.010]"))
                .Should()
                .Be(1);
            
            eventsMonitor
                .Should().Raise(nameof(fileDownloader.StateChanged))
                .WithSender(fileDownloader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileDownloaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileDownloader.DownloadCompleted))
                .WithSender(fileDownloader)
                .WithArgs<DownloadCompletedEventArgs>(args => args.Resource == remoteFilePath);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileDownloaderProxySpy120 : FileDownloader.FileDownloaderTestbed.MockedNativeFileDownloaderProxySpy
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

                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(00, 00);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(10, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(20, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(30, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(40, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(50, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(60, 10);

                    if (_tryCounter == _maxTriesCount && initialMtuSize != AndroidTidbits.BleConnectionFailsafeSettings.ForDownloading.InitialMtuSize)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(initialMtuSize)} set to {AndroidTidbits.BleConnectionFailsafeSettings.ForDownloading.InitialMtuSize} but it's set to {initialMtuSize?.ToString() ?? "(null)"} instead - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error); //                       order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EMcuMgrErrorCode.Unknown, EFileOperationGroupErrorCode.Unset); //   order
                        return;
                    }

                    if (_tryCounter < _maxTriesCount)
                    {
                        await Task.Delay(20);
                        StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Error); //                                   order
                        FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred", EMcuMgrErrorCode.Unknown, EFileOperationGroupErrorCode.Unset); //    order
                        return;
                    }

                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(70, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(80, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(90, 10);
                    await Task.Delay(5);
                    FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(100, 10);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileDownloaderState.Downloading, EFileDownloaderState.Complete); // order
                    DownloadCompletedAdvertisement(remoteFilePath, _expectedData); //                                              order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}