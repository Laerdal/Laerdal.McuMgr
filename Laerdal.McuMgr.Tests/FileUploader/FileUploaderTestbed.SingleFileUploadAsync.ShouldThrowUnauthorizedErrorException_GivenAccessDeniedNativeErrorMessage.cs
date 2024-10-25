using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldThrowUnauthorizedErrorException_GivenAccessDeniedNativeErrorMessage()
        {
            // Arrange
            var mockedNativeFileUploaderProxy = new MockedErroneousNativeFileUploaderProxySpy100(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                data: new byte[] { 1 },
                maxTriesCount: 2,
                remoteFilePath: "/path/to/file.bin"
            ));

            // Assert
            (await work.Should().ThrowExactlyAsync<AllUploadAttemptsFailedException>()).WithInnerExceptionExactly<UploadUnauthorizedException>();

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
                string remoteFilePath,
                byte[] data,
                int? pipelineDepth = null, //   ios only
                int? byteAlignment = null, //   ios only
                int? initialMtuSize = null, //  android only
                int? windowCapacity = null, //  android only
                int? memoryAlignment = null //  android only
            )
            {
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
                    await Task.Delay(100);

                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(100);
                    
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Error); //                               order
                    FatalErrorOccurredAdvertisement(remoteFilePath, "blah blah", EMcuMgrErrorCode.AccessDenied, EFileOperationGroupErrorCode.Unset); // order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}