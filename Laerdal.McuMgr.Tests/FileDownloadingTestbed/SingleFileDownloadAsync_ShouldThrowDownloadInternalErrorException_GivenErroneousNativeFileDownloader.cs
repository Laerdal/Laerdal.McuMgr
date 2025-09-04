using FluentAssertions;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using GenericNativeFileDownloaderCallbacksProxy_ = Laerdal.McuMgr.FileDownloading.FileDownloader.GenericNativeFileDownloaderCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task SingleFileDownloadAsync_ShouldThrowDownloadInternalErrorException_GivenErroneousNativeFileDownloader()
        {
            // Arrange
            var mockedNativeFileDownloaderProxy = new MockedErroneousNativeFileDownloaderProxySpy(new GenericNativeFileDownloaderCallbacksProxy_());
            var fileDownloader = new FileDownloader(mockedNativeFileDownloaderProxy);

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp.",
                
                remoteFilePath: "/path/to/file.bin"
            ));

            // Assert
            (await work.Should().ThrowExactlyAsync<FileDownloadInternalErrorException>()).WithInnerExceptionExactly<Exception>("native symbols not loaded blah blah");

            mockedNativeFileDownloaderProxy.CancelCalled.Should().BeFalse();
            mockedNativeFileDownloaderProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFileDownloaderProxy.BeginDownloadCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFileDownloaderProxySpy : MockedNativeFileDownloaderProxySpy
        {
            public MockedErroneousNativeFileDownloaderProxySpy(INativeFileDownloaderCallbacksProxy downloaderCallbacksProxy) : base(downloaderCallbacksProxy)
            {
            }

            public override EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, int? initialMtuSize = null)
            {
                base.NativeBeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize
                );

                Thread.Sleep(100);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}