using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions;
using Laerdal.McuMgr.DeviceResetter.Contracts.Native;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterTestbed
    {
        [Fact]
        public async Task ResetAsync_ShouldThrowTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedGreenButSlowNativeDeviceResetterProxySpy(new McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync(timeoutInMs: 100));

            // Assert
            await work
                .Should()
                .ThrowWithinAsync<DeviceResetTimeoutException>(5.Seconds());

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(deviceResetter.StateChanged))
                .WithSender(deviceResetter)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EDeviceResetterState.Resetting);

            eventsMonitor
                .Should().Raise(nameof(deviceResetter.StateChanged))
                .WithSender(deviceResetter)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EDeviceResetterState.Failed);

            eventsMonitor.Should().NotRaise(nameof(deviceResetter.FatalErrorOccurred));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenButSlowNativeDeviceResetterProxySpy : MockedNativeDeviceResetterProxySpy
        {
            public MockedGreenButSlowNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override EDeviceResetterInitializationVerdict BeginReset()
            {
                base.BeginReset();

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);

                    await Task.Delay(1_000);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Complete);
                });
                
                return EDeviceResetterInitializationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}