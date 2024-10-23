using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Events;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.SFUA.SCSBFBTFS.GFBC.010", "/path/to/file.bin", 2)]
        [InlineData("FUT.SFUA.SCSBFBTFS.GFBC.020", "/path/to/file.bin", 3)]
        [InlineData("FUT.SFUA.SCSBFBTFS.GFBC.030", "/path/to/file.bin", 5)]
        public async Task SingleFileUploadAsync_ShouldCompleteSuccessfullyByFallingBackToFailsafeSettings_GivenFlakyBluetoothConnection(string testcaseNickname, string remoteFilePath, int maxTriesCount)
        {
            // Arrange
            var stream = new MemoryStream([1, 2, 3]);
            
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy120(
                maxTriesCount: maxTriesCount,
                uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_()
            );
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                data: stream,
                maxTriesCount: maxTriesCount,
                remoteFilePath: remoteFilePath
            ));

            // Assert
            await work.Should().CompleteWithinAsync((maxTriesCount * 2).Seconds());
            
            mockedNativeFileUploaderProxy.BugDetected.Should().BeNull();
            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().HaveCount(maxTriesCount - 1); //one error for each try except the last one
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

            eventsMonitor
                .OccurredEvents
                .Where(x => x.EventName == nameof(fileUploader.LogEmitted))
                .SelectMany(x => x.Parameters)
                .OfType<LogEmittedEventArgs>()
                .Count(l => l is { Level: ELogLevel.Warning } && l.Message.Contains("GFCSICPTBU.010"))
                .Should()
                .Be(1);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.Resource == remoteFilePath && args.NewState == EFileUploaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FileUploaded))
                .WithSender(fileUploader)
                .WithArgs<FileUploadedEventArgs>(args => args.Resource == remoteFilePath);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy120 : MockedNativeFileUploaderProxySpy
        {
            private readonly int _maxTriesCount;

            public string BugDetected { get; private set; }

            public MockedGreenNativeFileUploaderProxySpy120(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, int maxTriesCount) : base(uploaderCallbacksProxy)
            {
                _maxTriesCount = maxTriesCount;
            }

            private int _tryCounter;
            public override EFileUploaderVerdict BeginUpload(
                string remoteFilePath,
                byte[] data,
                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only
                int? initialMtuSize = null, //  android only
                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                _tryCounter++;

                var verdict = base.BeginUpload(
                    data: data,
                    remoteFilePath: remoteFilePath,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    initialMtuSize: initialMtuSize, //   android only
                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(00, 00);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(20, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(30, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(40, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(50, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(60, 10);

                    if (_tryCounter == _maxTriesCount && initialMtuSize != AndroidTidbits.BleConnectionSettings.FailSafes.InitialMtuSize)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(initialMtuSize)} set to {AndroidTidbits.BleConnectionSettings.FailSafes.InitialMtuSize} but it's set to {initialMtuSize?.ToString() ?? "(null)"} instead - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                           order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EMcuMgrErrorCode.Generic, EFileOperationGroupReturnCode.Unset); // order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && windowCapacity != AndroidTidbits.BleConnectionSettings.FailSafes.WindowCapacity)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(windowCapacity)} set to {AndroidTidbits.BleConnectionSettings.FailSafes.WindowCapacity} but it's set to {windowCapacity?.ToString() ?? "(null)"} instead - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                           order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EMcuMgrErrorCode.Generic, EFileOperationGroupReturnCode.Unset); // order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && memoryAlignment != AndroidTidbits.BleConnectionSettings.FailSafes.MemoryAlignment)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(memoryAlignment)} set to {AndroidTidbits.BleConnectionSettings.FailSafes.MemoryAlignment} but it's set to {memoryAlignment?.ToString() ?? "(null)"} instead - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                           order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EMcuMgrErrorCode.Generic, EFileOperationGroupReturnCode.Unset); // order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && pipelineDepth != AppleTidbits.BleConnectionSettings.FailSafes.PipelineDepth)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(pipelineDepth)} set to {AppleTidbits.BleConnectionSettings.FailSafes.PipelineDepth} but it's set to {pipelineDepth?.ToString() ?? "(null)"} instead - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                           order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EMcuMgrErrorCode.Generic, EFileOperationGroupReturnCode.Unset); // order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && byteAlignment != AppleTidbits.BleConnectionSettings.FailSafes.ByteAlignment)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(byteAlignment)} set to {AppleTidbits.BleConnectionSettings.FailSafes.ByteAlignment} but it's set to {byteAlignment?.ToString() ?? "(null)"} instead - something is wrong!";
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                           order
                        FatalErrorOccurredAdvertisement(remoteFilePath, BugDetected, EMcuMgrErrorCode.Generic, EFileOperationGroupReturnCode.Unset); // order
                        return;
                    }

                    if (_tryCounter < _maxTriesCount)
                    {
                        await Task.Delay(20);
                        StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                                      order
                        FatalErrorOccurredAdvertisement(remoteFilePath, "fatal error occurred", EMcuMgrErrorCode.Generic, EFileOperationGroupReturnCode.Unset); // order
                        return;
                    }

                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(70, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(80, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(90, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 10);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete); // order
                    FileUploadedAdvertisement(remoteFilePath); //                                                            order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}