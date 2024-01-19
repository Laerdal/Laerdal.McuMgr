using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task EraseAsync_ShouldThrowTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenButSlowNativeFirmwareEraserProxySpy(new McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);
            using var eventsMonitor = firmwareEraser.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2, timeoutInMs: 100));

            // Assert
            await work.Should().ThrowExactlyAsync<FirmwareErasureTimeoutException>().WithTimeoutInMs((int)5.Seconds().TotalMilliseconds);

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(firmwareEraser.StateChanged))
                .WithSender(firmwareEraser)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareErasureState.Erasing);

            eventsMonitor
                .Should().Raise(nameof(firmwareEraser.StateChanged))
                .WithSender(firmwareEraser)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EFirmwareErasureState.Failed);

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenButSlowNativeFirmwareEraserProxySpy : MockedNativeFirmwareEraserProxySpy
        {
            public MockedGreenButSlowNativeFirmwareEraserProxySpy(INativeFirmwareEraserCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override void BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(oldState: EFirmwareErasureState.Idle, newState: EFirmwareErasureState.Erasing);

                    await Task.Delay(1_000);
                    StateChangedAdvertisement(oldState: EFirmwareErasureState.Erasing, newState: EFirmwareErasureState.Complete);
                });

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}