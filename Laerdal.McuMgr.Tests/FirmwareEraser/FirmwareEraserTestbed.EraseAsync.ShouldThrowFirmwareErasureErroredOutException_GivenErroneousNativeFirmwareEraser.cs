using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Native;
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
            
            using var eventsMonitor = firmwareEraser.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            (await work.Should().ThrowExactlyAsync<FirmwareErasureErroredOutException>()).WithInnerExceptionExactly<Exception>("native symbols not loaded blah blah");
            
            eventsMonitor
                .Should().Raise(nameof(firmwareEraser.StateChanged))
                .WithSender(firmwareEraser)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareErasureState.Failed);

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

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}