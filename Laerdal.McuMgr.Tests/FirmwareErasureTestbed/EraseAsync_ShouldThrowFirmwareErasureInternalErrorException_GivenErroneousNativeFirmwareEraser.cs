using FluentAssertions;
using Laerdal.McuMgr.FirmwareErasure;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Events;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareErasureTestbed
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task EraseAsync_ShouldThrowFirmwareErasureInternalErrorException_GivenErroneousNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedErroneousNativeFirmwareEraserProxySpy(new FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new FirmwareEraser(mockedNativeFirmwareEraserProxy);

            using var eventsMonitor = firmwareEraser.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            (await work.Should().ThrowExactlyAsync<FirmwareErasureInternalErrorException>()).WithInnerExceptionExactly<Exception>("native symbols not loaded blah blah");
            
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

            public override EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Thread.Sleep(100);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}