using FluentAssertions;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Exceptions;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using GenericNativeFileUploaderCallbacksProxy_ = Laerdal.McuMgr.FileUploader.FileUploader.GenericNativeFileUploaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public async Task SingleFileUploadAsync_ShouldThrowUploadInternalErrorException_GivenErroneousNativeFileUploader()
        {
            // Arrange
            var mockedNativeFileUploaderProxy = new MockedErroneousNativeFileUploaderProxySpy(new GenericNativeFileUploaderCallbacksProxy_());
            var fileUploader = new McuMgr.FileUploader.FileUploader(mockedNativeFileUploaderProxy);

            // Act
            var work = new Func<Task>(() => fileUploader.UploadAsync(localData: new byte[] { 1 }, remoteFilePath: "/path/to/file.bin"));

            // Assert
            (await work.Should().ThrowExactlyAsync<UploadInternalErrorException>()).WithInnerExceptionExactly<Exception>("native symbols not loaded blah blah");

            mockedNativeFileUploaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileUploaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileUploaderProxy.BeginUploadCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFileUploaderProxySpy : MockedNativeFileUploaderProxySpy
        {
            public MockedErroneousNativeFileUploaderProxySpy(INativeFileUploaderCallbacksProxy uploaderCallbacksProxy) : base(uploaderCallbacksProxy)
            {
            }

            public override EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                base.BeginUpload(remoteFilePath, data);

                Thread.Sleep(100);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}