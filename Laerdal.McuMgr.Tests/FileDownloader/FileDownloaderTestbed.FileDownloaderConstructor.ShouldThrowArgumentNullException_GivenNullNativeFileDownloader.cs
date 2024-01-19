using FluentAssertions;
using Laerdal.McuMgr.FileDownloader.Contracts;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderTestbed
    {
        [Fact]
        public void FileDownloaderConstructor_ShouldThrowArgumentNullException_GivenNullNativeFileDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IFileDownloader>(() => new McuMgr.FileDownloader.FileDownloader(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}