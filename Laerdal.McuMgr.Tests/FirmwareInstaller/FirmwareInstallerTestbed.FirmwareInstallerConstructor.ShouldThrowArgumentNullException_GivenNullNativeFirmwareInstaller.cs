using System;
using FluentAssertions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
{
    public partial class FirmwareInstallerTestbed
    {
        [Fact]
        public void FirmwareInstallerConstructor_ShouldThrowArgumentNullException_GivenNullNativeFirmwareInstaller()
        {
            // Arrange

            // Act
            var work = new Func<IFirmwareInstaller>(() => new McuMgr.FirmwareInstaller.FirmwareInstaller(nativeFirmwareInstallerProxy: null));

            // Assert
            work.Should().ThrowExactly<ArgumentNullException>();
        }
    }
}