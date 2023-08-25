using System;
using FluentAssertions;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public void FirmwareEraserConstructor_ShouldThrowArgumentNullException_GivenNullNativeFirmwareEraser()
        {
            // Arrange

            // Act
            var work = new Func<IFirmwareEraser>(() => new McuMgr.FirmwareEraser.FirmwareEraser(nativeFirmwareEraserProxy: null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}