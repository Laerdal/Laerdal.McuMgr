using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploading.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldThrowUnauthorizedErrorException_GivenAccessDeniedNativeErrorMessage()
        {
            // Arrange
            var mockedNativeFileUploaderProxy = new MockedErroneousNativeFileUploaderProxySpy100(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new FileUploader(mockedNativeFileUploaderProxy);

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                data: new byte[] { 1 },
                resourceId: "foobar",
                remoteFilePath: "/path/to/file.bin",
                
                maxTriesCount: 2
            ));

            // Assert
            (await work.Should().ThrowExactlyAsync<AllUploadAttemptsFailedException>())
                .WithInnerExceptionExactly<UploadUnauthorizedException>()
                .And
                .GlobalErrorCode.Should().Be(EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied);

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFileUploaderProxySpy100 : MockedNativeFileUploaderProxySpy
        {
            public MockedErroneousNativeFileUploaderProxySpy100(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy)
                : base(uploaderCallbacksProxy)
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
                    await Task.Delay(100);

                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);
                    FileUploadStartedAdvertisement(resourceId, remoteFilePath, data.Length);

                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(resourceId, remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                  order
                    FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, "blah blah", EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied); // order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}