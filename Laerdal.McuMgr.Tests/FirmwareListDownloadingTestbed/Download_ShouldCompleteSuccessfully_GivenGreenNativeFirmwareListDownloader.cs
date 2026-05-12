using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList;

#pragma warning disable xUnit1026

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        [Theory]
        [InlineData("FLDT.D.SCS.GGNFLD.010", null, null)]
        [InlineData("FLDT.D.SCS.GGNFLD.020", ELogLevel.Error, null)]
        [InlineData("FLDT.D.SCS.GGNFLD.030", ELogLevel.Trace, null)]
        [InlineData("FLDT.D.SCS.GGNFLD.040", null, 498)]
        [InlineData("FLDT.D.SCS.GGNFLD.050", ELogLevel.Info, 256)]
        public void Download_ShouldCompleteSuccessfully_GivenGreenNativeFirmwareListDownloader(string testcaseNickname, ELogLevel? minimumNativeLogLevel, int? initialMtuSize)
        {
            // Arrange
            var expectedJson = "[{\"version\":\"1.0.0\",\"slot\":0,\"active\":true}]";

            var mockedNativeFirmwareListDownloaderProxy = new MockedGreenNativeFirmwareListDownloaderProxySpy(expectedJson);
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var result = firmwareListDownloader.Download(
                minimumNativeLogLevel: minimumNativeLogLevel,
                initialMtuSize: initialMtuSize
            );

            // Assert
            result.Should().Be(expectedJson);
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        private class MockedGreenNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            private readonly string _mockedJsonResponse;

            public MockedGreenNativeFirmwareListDownloaderProxySpy(string mockedJsonResponse)
            {
                _mockedJsonResponse = mockedJsonResponse;
            }

            public override string DownloadFirmwareList(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.DownloadFirmwareList(initialMtuSize, minimumNativeLogLevel);

                return _mockedJsonResponse;
            }
        }
    }
}
