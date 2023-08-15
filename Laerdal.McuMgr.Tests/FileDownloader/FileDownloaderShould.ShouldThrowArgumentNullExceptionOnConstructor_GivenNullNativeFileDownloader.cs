using System;
using FluentAssertions;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.FileDownloader
{
    public partial class FileDownloaderShould
    {
        [Fact]
        public void ShouldThrowArgumentNullExceptionOnConstructor_GivenNullNativeFileDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IFileDownloader>(() => new McuMgr.FileDownloader.FileDownloader(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}