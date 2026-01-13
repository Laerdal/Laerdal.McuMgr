using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FirmwareErasure;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Events;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareErasureTestbed
{
    [SuppressMessage("Usage", "xUnit1030:Do not call ConfigureAwait(false) in test method")]
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task EraseAsync_ShouldThrowTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenButSlowNativeFirmwareEraserProxySpy(new FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new FirmwareEraser(mockedNativeFirmwareEraserProxy);
            using var eventsMonitor = firmwareEraser.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2, timeoutInMs: 100));

            // Assert
            await work.Should().ThrowWithinAsync<FirmwareErasureTimeoutException>(10.Seconds()).ConfigureAwait(false);

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

            public override EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10).ConfigureAwait(false);
                    StateChangedAdvertisement(oldState: EFirmwareErasureState.Idle, newState: EFirmwareErasureState.Erasing);

                    await Task.Delay(1_000).ConfigureAwait(false);
                    StateChangedAdvertisement(oldState: EFirmwareErasureState.Erasing, newState: EFirmwareErasureState.Complete);
                });
                
                return EFirmwareErasureInitializationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}