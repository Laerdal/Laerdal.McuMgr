using System;
using FluentAssertions;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterShould
    {
        [Fact]
        public void ShouldThrowArgumentNullExceptionOnConstructor_GivenNullNativeFileDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IDeviceResetter>(() => new McuMgr.DeviceResetter.DeviceResetter(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}