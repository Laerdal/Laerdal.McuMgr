using System;
using FluentAssertions;
using Laerdal.McuMgr.FileUploader.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.FileUploader
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public void FileUploaderConstructor_ShouldThrowArgumentNullException_GivenNullNativeFileUploader()
        {
            // Arrange

            // Act
            var work = new Func<IFileUploader>(() => new McuMgr.FileUploader.FileUploader(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}