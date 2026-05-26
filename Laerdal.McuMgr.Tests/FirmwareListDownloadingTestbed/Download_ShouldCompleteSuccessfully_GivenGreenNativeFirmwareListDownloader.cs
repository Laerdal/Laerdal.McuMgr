using FluentAssertions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareList;
using Laerdal.McuMgr.FirmwareList.Model;

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
            const string expectedResult = """[{"version":"1.0.0","slot":0,"active":true,"bootable":false,"compressed":false,"confirmed":false,"image_number":0,"permanent":true,"pending":false,"hash":"abc"}]""";
            List<InstalledFirmwareInformation> expectedList =
            [
                new InstalledFirmwareInformation(
                    Version: "1.0.0",
                    Slot: 0,
                    IsActive: true,
                    IsBootable: false,
                    IsCompressed: false,
                    IsConfirmed: false,
                    ImageNumber: 0,
                    IsPermanent: true,
                    IsPending: false,
                    Hash: "abc"
                )
            ];


            var mockedNativeFirmwareListDownloaderProxy = new MockedGreenNativeFirmwareListDownloaderProxySpy(expectedResult);
            var firmwareListDownloader = new FirmwareListDownloader(mockedNativeFirmwareListDownloaderProxy);

            // Act
            var result = firmwareListDownloader.Get(
                minimumNativeLogLevel: minimumNativeLogLevel,
                initialMtuSize: initialMtuSize
            );

            // Assert
            result.Should().BeEquivalentTo(expectedList);
            mockedNativeFirmwareListDownloaderProxy.DownloadFirmwareListCalled.Should().BeTrue();
        }

        private class MockedGreenNativeFirmwareListDownloaderProxySpy : MockedNativeFirmwareListDownloaderProxySpy
        {
            private readonly string _mockedJsonResponse;

            public MockedGreenNativeFirmwareListDownloaderProxySpy(string mockedJsonResponse)
            {
                _mockedJsonResponse = mockedJsonResponse;
            }

            public override string Get(int initialMtuSize, ELogLevel minimumNativeLogLevel)
            {
                base.Get(initialMtuSize, minimumNativeLogLevel);

                return _mockedJsonResponse;
            }
        }
    }
}