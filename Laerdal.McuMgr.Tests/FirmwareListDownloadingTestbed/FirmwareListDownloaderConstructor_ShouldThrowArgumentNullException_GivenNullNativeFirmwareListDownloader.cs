using FluentAssertions;
using Laerdal.McuMgr.FirmwareList;
using Laerdal.McuMgr.FirmwareList.Contracts;

namespace Laerdal.McuMgr.Tests.FirmwareListDownloadingTestbed
{
    public partial class FirmwareListDownloaderTestbed
    {
        [Fact]
        public void FirmwareListDownloaderConstructor_ShouldThrowArgumentNullException_GivenNullNativeFirmwareListDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IFirmwareListDownloader>(() => new FirmwareListDownloader(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}
