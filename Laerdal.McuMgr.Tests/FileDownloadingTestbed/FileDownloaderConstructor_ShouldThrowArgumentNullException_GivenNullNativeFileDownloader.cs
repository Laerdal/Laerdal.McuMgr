using FluentAssertions;
using Laerdal.McuMgr.FileDownloading;
using Laerdal.McuMgr.FileDownloading.Contracts;

namespace Laerdal.McuMgr.Tests.FileDownloadingTestbed
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public void FileDownloaderConstructor_ShouldThrowArgumentNullException_GivenNullNativeFileDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IFileDownloader>(() => new FileDownloader(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}