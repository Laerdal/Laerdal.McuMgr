using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Exceptions;
using Xunit;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public async Task ShouldThrowFirmwareErasureErroredOutExceptionOnDownloadAsync_GivenErroneousNativeFileDownloader()
        {
            // Arrange
            var mockedNativeFileDownloaderProxy = new MockedErroneousNativeFileDownloaderProxySpy(new McuMgr.FileDownloader.FileDownloader.GenericNativeFileDownloaderCallbacksProxy());
            var fileDownloader = new McuMgr.FileDownloader.FileDownloader(mockedNativeFileDownloaderProxy);

            // Act
            var work = new Func<Task>(() => fileDownloader.DownloadAsync(remoteFilePath: "/path/to/file.bin"));

            // Assert
            (await work.Should().ThrowAsync<DownloadErroredOutException>()).WithInnerExceptionExactly<Exception>("foobar");

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

            public override EFileDownloaderVerdict BeginDownload(string remoteFilePath)
            {
                base.BeginDownload(remoteFilePath);

                Thread.Sleep(100);

                throw new Exception("foobar");
            }
        }
    }
}