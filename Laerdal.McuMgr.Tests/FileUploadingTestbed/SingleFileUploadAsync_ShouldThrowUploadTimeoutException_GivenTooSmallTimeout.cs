using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldThrowUploadTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            const string remoteFilePath = "/path/to/file.bin";

            var resourceId = "foobar";
            var mockedNativeFileUploaderProxy = new MockedGreenButSlowNativeFileUploaderProxySpy(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                data: new byte[] {1},
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,

                timeoutForUploadInMs: 100
            ));

            // Assert
            await work.Should().ThrowWithinAsync<FileUploadTimeoutException>(5.Seconds());

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(fileUploader.StateChanged))
                .WithSender(fileUploader)
                .WithArgs<StateChangedEventArgs>(args => args.ResourceId == resourceId && args.RemoteFilePath == remoteFilePath && args.NewState == EFileUploaderState.Error);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenButSlowNativeFileUploaderProxySpy : BaseMockedNativeFileUploaderProxySpy
        {
            public MockedGreenButSlowNativeFileUploaderProxySpy(INativeFileUploaderCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict NativeBeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                
                ELogLevel? minimumNativeLogLevel = null,
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
                    minimumNativeLogLevel: minimumNativeLogLevel,
                    
                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );

                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.None, 0);
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.Idle, 0);

                _ = StartUploadAsync_(); //00 vital  simulate state changes in the background without hard-blocking the current thread

                return EFileUploaderVerdict.Success;
                
                async Task StartUploadAsync_()
                {
                    StateChangedAdvertisement(resourceId, remoteFilePath, oldState: EFileUploaderState.Idle, newState: EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);
                    await Task.Delay(1_000);
                    StateChangedAdvertisement(resourceId, remoteFilePath, oldState: EFileUploaderState.Uploading, newState: EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                }

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}