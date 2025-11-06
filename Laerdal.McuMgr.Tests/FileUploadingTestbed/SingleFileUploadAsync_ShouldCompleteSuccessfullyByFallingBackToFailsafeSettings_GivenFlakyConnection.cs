using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
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
            var resourceId = "some_resource.txt";
            var allLogEas = new List<LogEmittedEventArgs>(8);
            
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy120(
                maxTriesCount: maxTriesCount,
                uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_()
            );
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() =>
            {
                fileUploader.LogEmitted += (object _, in LogEmittedEventArgs ea) => allLogEas.Add(ea);
                
                return fileUploader.UploadAsync(
                    hostDeviceModel: "foobar",
                    hostDeviceManufacturer: "acme corp.",
                    
                    data: stream,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    maxTriesCount: maxTriesCount
                );
            });

            // Assert
            await work.Should().CompleteWithinAsync((maxTriesCount * 2).Seconds());
            
            mockedNativeFileUploaderProxy.BugDetected.Should().BeNull();
            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor
                .OccurredEvents
                .Where(x => x.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().HaveCount(maxTriesCount - 1); //one error for each try except the last one
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

            // eventsMonitor
            //     .OccurredEvents
            //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
            //     .SelectMany(x => x.Parameters)
            //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(

            allLogEas
                .Count(l => l is {Level: ELogLevel.Warning} && l.Message.Contains("[FU.UA.010]", StringComparison.InvariantCulture))
                .Should()
                .Be(1);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Complete);

            eventsMonitor
                .Should().Raise(nameof(fileUploader.FileUploadStarted))
                .WithSender(fileUploader)
                .WithArgs<FileUploadStartedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.FileUploadCompleted))
                .WithSender(fileUploader)
                .WithArgs<FileUploadCompletedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy120 : BaseMockedNativeFileUploaderProxySpy
        {
            private readonly int _maxTriesCount;

            public string BugDetected { get; private set; }

            public MockedGreenNativeFileUploaderProxySpy120(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy, int maxTriesCount) : base(uploaderCallbacksProxy)
            {
                _maxTriesCount = maxTriesCount;
            }

            private int _tryCounter;
            public override EFileUploaderVerdict NativeBeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                
                ELogLevel? minimumLogLevel = null,
                int? initialMtuSize = null,

                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only

                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                _tryCounter++;

                base.NativeBeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,

                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );
                
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.None, 0);
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.Idle, 0);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);

                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 00, 00, 00);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 10, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 20, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 30, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 40, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 50, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 60, 10, 10);

                    if (_tryCounter == _maxTriesCount && initialMtuSize == null)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(initialMtuSize)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0); //  order
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, BugDetected, EGlobalErrorCode.Generic); //                                        order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && windowCapacity == null)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(windowCapacity)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0); //  order
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, BugDetected, EGlobalErrorCode.Generic); //                                        order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && memoryAlignment == null)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(memoryAlignment)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0); // order
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, BugDetected, EGlobalErrorCode.Generic); //                                       order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && pipelineDepth == null)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(pipelineDepth)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0); // order
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, BugDetected, EGlobalErrorCode.Generic); //                                       order
                        return;
                    }

                    if (_tryCounter == _maxTriesCount && byteAlignment == null)
                    {
                        BugDetected = $"[BUG DETECTED] The very last try should be with {nameof(byteAlignment)} set to a fail-safe value but it's still set to 'null' - something is wrong!";
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0); // order
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, BugDetected, EGlobalErrorCode.Generic); //                                       order
                        return;
                    }

                    if (_tryCounter < _maxTriesCount)
                    {
                        await Task.Delay(20);
                        StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error, totalBytesToBeUploaded: 0); // order
                        FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "fatal error occurred", EGlobalErrorCode.Generic); //                            order
                        return;
                    }

                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 70, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 80, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 90, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 100, 10, 10);
                    
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: data.Length);
                });

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}