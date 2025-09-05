using FluentAssertions;
using FluentAssertions.Extensions;
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
        [InlineData("FUT.SFUA.SCAGS.GBAP.010", "stream", true)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.020", "stream", false)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.030", "func_stream", true)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.040", "func_stream", false)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.050", "func_task_stream", true)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.060", "func_task_stream", false)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.070", "func_valuetask_stream", true)]
        [InlineData("FUT.SFUA.SCAGS.GBAP.080", "func_valuetask_stream", false)]
        public async Task SingleFileUploadAsync_ShouldConditionallyAutodisposeGivenStream_GivenBooleanAutodisposedParameter(string testcaseNickname, string streamType, bool shouldAutodisposeStream)
        {
            // Arrange
            var stream = new MemoryStream([1, 2, 3]);
            var resourceId = "some_resource.txt";
            var streamProvider = streamType switch
            {
                "stream" => (object)stream,
                "func_stream" => () => stream,
                "func_task_stream" => () => Task.FromResult<Stream>(stream),
                "func_valuetask_stream" => () => new ValueTask<Stream>(stream),
                _ => throw new NotImplementedException($"Wops! Don't know how to handle stream type {streamType}! (how did this happen?)")
            };

            const string remoteFilePath = "/foo/bar/ping.bin";

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy110(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                data: streamProvider,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,
                
                maxTriesCount: 1,
                autodisposeStream: shouldAutodisposeStream
            ));

            // Assert
            await work.Should().CompleteWithinAsync(10.Seconds());

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();
            
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
            
            stream.CanRead.Should().Be(!shouldAutodisposeStream); //10

            //00 we dont want to disconnect the device regardless of the outcome
            //10 check if the stream was disposed or not based on the value of the autodisposeStream parameter
        }

        private class MockedGreenNativeFileUploaderProxySpy110 : BaseMockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy110(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict NativeBeginUpload(
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
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 70, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 80, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 90, 10, 10);
                    await Task.Delay(5);
                    FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, 100, 10, 10);

                    await Task.Delay(20);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                });

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}