using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Constants;
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
        [Theory] //@formatter:off
        [InlineData("FUT.SFUA.SCSBFBTFSRA.GPD.010", "sm-x200    ", "  samsung  ", null, null, null, null, null, null, null,   23,    1,    1)]
        [InlineData("FUT.SFUA.SCSBFBTFSRA.GPD.020", "  SM-X200  ", "  Samsung  ", null, null, null, null, null, null, null,   23,    1,    1)]
        [InlineData("FUT.SFUA.SCSBFBTFSRA.GPD.030", "  iPhone 6 ", "  Apple    ", null, null, null, null, null,    1,    0, null, null, null)]
        [InlineData("FUT.SFUA.SCSBFBTFSRA.GPD.040", "  iPhone 6 ", "  Apple    ",    2,    4, null, null, null,    2,    4, null, null, null)]
        [InlineData("FUT.SFUA.SCSBFBTFSRA.GPD.050", "  foobar   ", " AcmeCorp. ", null, null, null, null, null, null, null, null, null, null)]
        public async Task SingleFileUploadAsync_ShouldCompleteSuccessfullyByFallingBackToFailsafeSettingsRightAway_GivenProblematicDevices( //@formatter:on
            string testcaseNickname,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            
            int? pipelineDepth,
            int? byteAlignment,
            int? initialMtuSize,
            int? windowCapacity,
            int? memoryAlignment,
            
            int? expectedPipelineDepth,
            int? expectedByteAlignment,
            int? expectedInitialMtuSize,
            int? expectedWindowCapacity,
            int? expectedMemoryAlignment
        )
        {
            // Arrange
            var stream = new MemoryStream([1, 2, 3]);
            var resourceId = "foobar";
            var remoteFilePath = "/foo/bar/ping.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy140(uploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);
            
            AppleTidbits.KnownProblematicDevices.Add(("iphone 6", "apple"));

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,
                
                data: stream,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,
                
                maxTriesCount: 1,
                initialMtuSize: initialMtuSize,
                
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            ));

            // Assert
            await work.Should().CompleteWithinAsync(200.Seconds());
            
            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();
            
            mockedNativeFileUploaderProxy.ObservedPipelineDepth.Should().Be(expectedPipelineDepth);
            mockedNativeFileUploaderProxy.ObservedByteAlignment.Should().Be(expectedByteAlignment);
            
            mockedNativeFileUploaderProxy.ObservedWindowCapacity.Should().Be(expectedWindowCapacity);
            mockedNativeFileUploaderProxy.ObservedInitialMtuSize.Should().Be(expectedInitialMtuSize);
            mockedNativeFileUploaderProxy.ObservedMemoryAlignment.Should().Be(expectedMemoryAlignment);

            eventsMonitor
                .OccurredEvents.Where(x => x.EventName == nameof(fileUploader.FatalErrorOccurred))
                .Should().HaveCount(0);
            
            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Uploading);

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

        private class MockedGreenNativeFileUploaderProxySpy140 : MockedNativeFileUploaderProxySpy
        {
            public int? ObservedPipelineDepth { get; private set; }
            public int? ObservedByteAlignment { get; private set; }

            public int? ObservedInitialMtuSize { get; private set; }
            public int? ObservedWindowCapacity { get; private set; }
            public int? ObservedMemoryAlignment { get; private set; }

            public MockedGreenNativeFileUploaderProxySpy140(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                int? initialMtuSize = null,

                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only

                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                ObservedPipelineDepth = pipelineDepth;
                ObservedByteAlignment = byteAlignment;
                    
                ObservedInitialMtuSize = initialMtuSize;
                ObservedWindowCapacity = windowCapacity;
                ObservedMemoryAlignment = memoryAlignment;
                
                var verdict = base.BeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);
                    FileUploadStartedAdvertisement(resourceId, remoteFilePath, data.Length);
                    
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
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 70, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 80, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 90, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 100, 10, 10);

                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete); // order
                    FileUploadCompletedAdvertisement(resourceId, remoteFilePath); //                                                     order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}