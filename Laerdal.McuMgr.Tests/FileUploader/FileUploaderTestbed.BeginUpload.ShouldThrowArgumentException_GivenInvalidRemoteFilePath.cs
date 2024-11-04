using FluentAssertions;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Theory]
        [InlineData("FUT.BD.STAE.GIRFP.010", "")]
        [InlineData("FUT.BD.STAE.GIRFP.020", null)]
        [InlineData("FUT.BD.STAE.GIRFP.030", "foo/bar/")] //  paths are not allowed
        [InlineData("FUT.BD.STAE.GIRFP.040", "/foo/bar/")] // to end with a slash 
        public void BeginUpload_ShouldThrowArgumentException_GivenInvalidRemoteFilePath(string testcaseNickname, string remoteFilePath)
        {
            // Arrange
            var mockedFileData = new byte[] { 1, 2, 3 };

            var mockedNativeFileUploaderProxy = new MockedGreenNativeFileUploaderProxySpy1(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            using var eventsMonitor = fileUploader.Monitor();

            // Act
            var work = new Func<EFileUploaderVerdict>(() => fileUploader.BeginUpload(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",

                data: mockedFileData,
                remoteFilePath: remoteFilePath
            ));

            // Assert
            work.Should().ThrowExactly<ArgumentException>();

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeFalse();

            eventsMonitor.Should().NotRaise(nameof(fileUploader.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(fileUploader.FileUploaded));

            //00 we dont want to disconnect the device regardless of the outcome
        }
        
        private class MockedGreenNativeFileUploaderProxySpy1 : MockedNativeFileUploaderProxySpy
        {
            public MockedGreenNativeFileUploaderProxySpy1(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
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
                    await Task.Delay(10);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Idle, EFileUploaderState.Uploading);

                    await Task.Delay(20);
                    StateChangedAdvertisement(remoteFilePath, EFileUploaderState.Uploading, EFileUploaderState.Complete); //   order
                    FileUploadedAdvertisement(remoteFilePath); //                                                              order
                });

                return verdict;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
            }
        }
    }
}