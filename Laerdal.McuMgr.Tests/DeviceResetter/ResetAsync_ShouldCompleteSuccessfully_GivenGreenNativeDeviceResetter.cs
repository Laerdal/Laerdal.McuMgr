using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetter.Contracts.Native;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterTestbed
    {
        [Fact]
        public async Task ResetAsync_ShouldCompleteSuccessfully_GivenGreenNativeDeviceResetter()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedGreenNativeDeviceResetterProxySpy1(new McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);

            using var eventsMonitor = deviceResetter.Monitor();
            deviceResetter.LogEmitted += (object _, in LogEmittedEventArgs _) => throw new Exception($"{nameof(deviceResetter.LogEmitted)} -> oops!"); //library should be immune to any and all user-land exceptions 
            deviceResetter.StateChanged += (_, _) => throw new Exception($"{nameof(deviceResetter.StateChanged)} -> oops!");
            deviceResetter.FatalErrorOccurred += (_, _) => throw new Exception($"{nameof(deviceResetter.FatalErrorOccurred)} -> oops!");

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            await work.Should().CompleteWithinAsync(5.Seconds());

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            eventsMonitor.Should().Raise(nameof(deviceResetter.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(deviceResetter.FatalErrorOccurred));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeDeviceResetterProxySpy1 : MockedNativeDeviceResetterProxySpy
        {
            public MockedGreenNativeDeviceResetterProxySpy1(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override EDeviceResetterInitializationVerdict BeginReset()
            {
                base.BeginReset();

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);

                    await Task.Delay(20);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Complete);
                });
                
                return EDeviceResetterInitializationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }
    }
}