using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList;

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        [Fact]
        public void Download_ShouldThrowException_GivenErroneousNativeFirmwareListDownloader()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedErroneousNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var work = new Action(() => firmwareListDownloader.Download());

            // Assert
            work.Should().ThrowExactly<Exception>().WithMessage("native symbols not loaded blah blah");

            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        private class MockedErroneousNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            public override string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.DownloadFirmwareList(initialMtuSize, minimumNativeLogLevel);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}
