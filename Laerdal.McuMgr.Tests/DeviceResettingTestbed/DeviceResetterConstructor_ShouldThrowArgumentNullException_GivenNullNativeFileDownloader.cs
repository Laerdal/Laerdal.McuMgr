using FluentAssertions;
using Laerdal.McuMgr.DeviceResetting;
using Laerdal.McuMgr.DeviceResetting.Contracts;

namespace Laerdal.McuMgr.Tests.DeviceResettingTestbed
{
    public partial class DeviceResetterTestbed
    {
        [Fact]
        public void DeviceResetterConstructor_ShouldThrowArgumentNullException_GivenNullNativeFileDownloader()
        {
            // Arrange

            // Act
            var work = new Func<IDeviceResetter>(() => new DeviceResetter(null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}