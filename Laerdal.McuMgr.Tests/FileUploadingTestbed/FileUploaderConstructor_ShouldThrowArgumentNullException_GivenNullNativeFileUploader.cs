using FluentAssertions;
using Laerdal.McuMgr.FileUploading;
using Laerdal.McuMgr.FileUploading.Contracts;

namespace Laerdal.McuMgr.Tests.FileUploadingTestbed
{
    public partial class FileUploaderTestbed
    {
        [Fact]
        public void FileUploaderConstructor_ShouldThrowArgumentNullException_GivenNullNativeFileUploader()
        {
            // Arrange

            // Act
            var work = new Func<IFileUploader>(() => new FileUploader(nativeFileUploaderProxy: null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}