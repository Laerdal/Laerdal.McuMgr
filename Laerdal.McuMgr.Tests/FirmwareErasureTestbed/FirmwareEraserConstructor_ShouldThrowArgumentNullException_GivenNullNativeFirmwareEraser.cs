using FluentAssertions;
using Laerdal.McuMgr.FirmwareErasure;
using Laerdal.McuMgr.FirmwareErasure.Contracts;

namespace Laerdal.McuMgr.Tests.FirmwareErasureTestbed
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public void FirmwareEraserConstructor_ShouldThrowArgumentNullException_GivenNullNativeFirmwareEraser()
        {
            // Arrange

            // Act
            var work = new Func<IFirmwareEraser>(() => new FirmwareEraser(nativeFirmwareEraserProxy: null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}