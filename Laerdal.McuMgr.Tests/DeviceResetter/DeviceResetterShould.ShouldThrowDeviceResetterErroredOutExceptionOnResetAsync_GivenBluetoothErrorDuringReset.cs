using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions;
using Xunit;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterShould
    {
        [Fact]
        public async Task ShouldThrowDeviceResetterErroredOutExceptionOnResetAsync_GivenBluetoothErrorDuringReset()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy(new McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            await work
                .Should().ThrowExactlyAsync<DeviceResetterErroredOutException>()
                .WithTimeoutInMs(100)
                .WithMessage("*bluetooth error blah blah*");

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(deviceResetter.StateChanged))
                .WithSender(deviceResetter)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EDeviceResetterState.Failed);

            eventsMonitor
                .Should().Raise(nameof(deviceResetter.FatalErrorOccurred))
                .WithSender(deviceResetter)
                .WithArgs<FatalErrorOccurredEventArgs>(args => args.ErrorMessage == "bluetooth error blah blah");

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy : MockedNativeDeviceResetterProxySpy
        {
            public MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override void BeginReset()
            {
                base.BeginReset();

                Task.Run(async () => //00
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);

                    await Task.Delay(20);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Failed);
                    FatalErrorOccurredAdvertisement("bluetooth error blah blah");

                    //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
                });
            }
        }
    }
}