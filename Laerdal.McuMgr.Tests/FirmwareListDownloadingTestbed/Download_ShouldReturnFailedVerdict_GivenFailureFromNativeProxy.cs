using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList;

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        [Fact]
        public void Download_ShouldThrowException_GivenInvalidSettingsFromNativeProxy()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedInvalidSettingsNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var work = new Action(() => firmwareListDownloader.Get());

            // Assert
            work.Should().ThrowExactly<InvalidOperationException>().WithMessage("*FailedInvalidSettings*");
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        [Fact]
        public void Download_ShouldThrowException_GivenInvalidDataFromNativeProxy()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedInvalidDataNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var work = new Action(() => firmwareListDownloader.Get());

            // Assert
            work.Should().ThrowExactly<InvalidOperationException>().WithMessage("*FailedInvalidData*");
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        private class MockedInvalidSettingsNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            public override string Get(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.Get(initialMtuSize, minimumNativeLogLevel);

                throw new InvalidOperationException("Native call failed with verdict 'FailedInvalidSettings'");
            }
        }

        private class MockedInvalidDataNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            public override string Get(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.Get(initialMtuSize, minimumNativeLogLevel);

                throw new InvalidOperationException("Native call failed with verdict 'FailedInvalidData'");
            }
        }
    }
}
