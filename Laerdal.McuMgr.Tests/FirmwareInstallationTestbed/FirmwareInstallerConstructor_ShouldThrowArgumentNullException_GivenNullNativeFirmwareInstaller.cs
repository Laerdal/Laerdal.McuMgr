using FluentAssertions;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public void FirmwareInstallerConstructor_ShouldThrowArgumentNullException_GivenNullNativeFirmwareInstaller()
        {
            // Arrange

            // Act
            var work = new Func<IFirmwareInstaller>(() => new FirmwareInstaller(nativeFirmwareInstallerProxy: null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}