using FluentAssertions;
using Laerdal.McuMgr.DeviceResetter.Contracts;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterTestbed
    {
        [Fact]
        public void DeviceResetterConstructor_ShouldThrowArgumentNullException_GivenNullNativeFileDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IDeviceResetter>(() => new McuMgr.DeviceResetter.DeviceResetter(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}