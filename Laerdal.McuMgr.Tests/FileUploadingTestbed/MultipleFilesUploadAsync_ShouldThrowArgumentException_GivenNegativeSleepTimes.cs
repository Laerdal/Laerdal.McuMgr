using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.MFDA.STAE.GNST.010", -1, 0)]
        [InlineData("FUT.MFDA.STAE.GNST.020", 0, -1)]
        public async Task MultipleFilesUploadAsync_ShouldThrowArgumentOutOfRangeException_GivenNegativeSleepTimes(string testcaseNickname, int sleepTimeBetweenUploadsInMs, int sleepTimeBetweenRetriesInMs)
        {
            // Arrange
            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy12(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<Task>(async () => await fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                remoteFilePathsAndTheirData: new Dictionary<string, (string, byte[])> { { "/foo/bar.bin", ("/xyz/bar.bin", [1, 2, 3]) } },

                sleepTimeBetweenUploadsInMs: sleepTimeBetweenUploadsInMs,
                sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs
            ));

            // Assert
            (await work.Should().ThrowWithinAsync<ArgumentOutOfRangeException>(500.Milliseconds())).WithMessage("*sleepTimeBetween*InMs*");

            eventsMonitor.OccurredEvents.Should().HaveCount(0);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeFalse();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFileUploaderProxySpy12 : BaseMockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy12(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict NativeBeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                
                ELogLevel? minimumNativeLogLevel = null,

                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only
                int? initialMtuSize = null, //  android only
                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
                base.NativeBeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,

                    pipelineDepth: pipelineDepth, //     ios only
                    byteAlignment: byteAlignment, //     ios only

                    initialMtuSize: initialMtuSize, //   android only
                    windowCapacity: windowCapacity, //   android only
                    memoryAlignment: memoryAlignment //  android only
                );
                
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.None, 0);
                StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.None, EFileUploaderState.Idle, 0);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading, totalBytesToBeUploaded: data.Length);
                    
                    await Task.Delay(20);
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete, totalBytesToBeUploaded: 0);
                });

                return EFileUploaderVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}