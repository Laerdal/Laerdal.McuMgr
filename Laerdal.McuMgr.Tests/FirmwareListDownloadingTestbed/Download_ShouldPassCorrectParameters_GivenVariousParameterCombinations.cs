using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList;

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        [Fact]
        public void Download_ShouldPassCorrectDefaultParameters_GivenNoExplicitParameters()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedParameterCapturingNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            firmwareListDownloader.Download();

            // Assert
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
            mockedNativeFirmwareListDownloaderProxy.CapturedInitialMtuSize.Should().Be(-1);
            mockedNativeFirmwareListDownloaderProxy.CapturedMinimumNativeLogLevel.Should().Be(ELogLevel.Error);
        }

        [Fact]
        public void Download_ShouldPassCorrectExplicitParameters_GivenExplicitParameters()
        {
            // Arrange
            var mockedNativeFirmwareListDownloaderProxy = new MockedParameterCapturingNativeFirmwareListDownloaderProxySpy();
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            firmwareListDownloader.Download(
                minimumNativeLogLevel: ELogLevel.Trace,
                initialMtuSize: 256
            );

            // Assert
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
            mockedNativeFirmwareListDownloaderProxy.CapturedInitialMtuSize.Should().Be(256);
            mockedNativeFirmwareListDownloaderProxy.CapturedMinimumNativeLogLevel.Should().Be(ELogLevel.Trace);
        }

        private class MockedParameterCapturingNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            public int CapturedInitialMtuSize { get; private set; }
            public ELogLevel CapturedMinimumNativeLogLevel { get; private set; }

            public override string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.DownloadFirmwareList(initialMtuSize, minimumNativeLogLevel);

                CapturedInitialMtuSize = initialMtuSize;
                CapturedMinimumNativeLogLevel = minimumNativeLogLevel;

                return "[]";
            }
        }
    }
}
