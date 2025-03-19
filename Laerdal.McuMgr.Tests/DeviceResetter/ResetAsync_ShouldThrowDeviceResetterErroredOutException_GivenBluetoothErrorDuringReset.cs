using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions;
using Laerdal.McuMgr.DeviceResetter.Contracts.Native;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterTestbed
    {
        [Fact]
        public async Task ResetAsync_ShouldThrowDeviceResetterErroredOutException_GivenBluetoothErrorDuringReset()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy(new McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            await work
                .Should().ThrowWithinAsync<DeviceResetterErroredOutException>(500.Milliseconds())
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

            eventsMonitor
                .Should().Raise(nameof(deviceResetter.LogEmitted))
                .WithSender(deviceResetter)
                .WithArgs<LogEmittedEventArgs>(args => args.Level == ELogLevel.Error && args.Message.Contains("bluetooth error blah blah"));
            
            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy : MockedNativeDeviceResetterProxySpy
        {
            public MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override EDeviceResetterInitializationVerdict BeginReset()
            {
                base.BeginReset();

                Task.Run(async () => //00
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);

                    await Task.Delay(20);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Failed);
                    FatalErrorOccurredAdvertisement("bluetooth error blah blah", EGlobalErrorCode.Generic);

                    //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
                });
                
                return EDeviceResetterInitializationVerdict.Success;
            }
        }
    }
}