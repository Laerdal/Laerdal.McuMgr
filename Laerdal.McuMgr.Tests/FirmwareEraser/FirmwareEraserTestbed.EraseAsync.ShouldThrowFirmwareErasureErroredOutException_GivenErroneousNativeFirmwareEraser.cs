using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions;
using Xunit;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task EraseAsync_ShouldThrowFirmwareErasureErroredOutException_GivenErroneousNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedErroneousNativeFirmwareEraserProxySpy(new McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            (await work.Should().ThrowAsync<FirmwareErasureErroredOutException>()).WithInnerExceptionExactly<Exception>("foobar");

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFirmwareEraserProxySpy : MockedNativeFirmwareEraserProxySpy
        {
            public MockedErroneousNativeFirmwareEraserProxySpy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy) : base(eraserCallbacksProxy)
            {
            }

            public override void BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Thread.Sleep(100);

                throw new Exception("foobar");
            }
        }
    }
}