using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
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
            var allLogsEas = new List<LogEmittedEventArgs>(8);
            var mockedNativeDeviceResetterProxy = new MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy(new McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() =>
            {
                deviceResetter.LogEmitted += (object _, in LogEmittedEventArgs ea) => allLogsEas.Add(ea);
                
                return deviceResetter.ResetAsync();
            });

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
            
            // eventsMonitor
            //     .OccurredEvents
            //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
            //     .SelectMany(x => x.Parameters)
            //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(
                
            allLogsEas
                .Count(l => l is { Level: ELogLevel.Error } && l.Message.Contains("bluetooth error blah blah", StringComparison.InvariantCulture))
                .Should()
                .BeGreaterOrEqualTo(1);
            
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