using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList;

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        [Fact]
        public void Download_ShouldReturnFailedVerdict_GivenInvalidSettingsFromNativeProxy()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedInvalidSettingsNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var result = firmwareListDownloader.Download();

            // Assert
            result.Should().Be("FAILED__INVALID_SETTINGS");
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        [Fact]
        public void Download_ShouldReturnFailedVerdict_GivenInvalidDataFromNativeProxy()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedInvalidDataNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var result = firmwareListDownloader.Download();

            // Assert
            result.Should().Be("FAILED__INVALID_DATA");
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        private class MockedInvalidSettingsNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            public override string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.DownloadFirmwareList(initialMtuSize, minimumNativeLogLevel);

                return "FAILED__INVALID_SETTINGS";
            }
        }

        private class MockedInvalidDataNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            public override string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.DownloadFirmwareList(initialMtuSize, minimumNativeLogLevel);

                return "FAILED__INVALID_DATA";
            }
        }
    }
}
