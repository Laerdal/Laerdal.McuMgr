using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions;
using Xunit;
using GenericNativeDeviceResetterCallbacksProxy_ = Laerdal.McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public class DeviceResetterShould
    {
        [Fact]
        public async Task ShouldCompleteSuccessfully_GivenGreenNativeDeviceResetter()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedGreenNativeDeviceResetterProxySpy(new GenericNativeDeviceResetterCallbacksProxy_());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            await work.Should().CompleteWithinAsync(0.5.Seconds());

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();
            
            eventsMonitor.Should().Raise(nameof(deviceResetter.StateChanged));
            eventsMonitor.Should().NotRaise(nameof(deviceResetter.FatalErrorOccurred));

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeDeviceResetterProxySpy : MockedNativeDeviceResetterProxySpy
        {
            public MockedGreenNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override void BeginReset()
            {
                base.BeginReset();

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);

                    Task.Delay(20).GetAwaiter().GetResult();
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Complete);
                });

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }

        [Fact]
        public async Task ShouldThrowDeviceResetterErroredOutException_GivenErroneousDueToMissingNativeSymbolsNativeDeviceResetterProxy()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedErroneousDueToMissingSymbolsNativeDeviceResetterProxySpy(new GenericNativeDeviceResetterCallbacksProxy_());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            (
                await work
                    .Should().ThrowExactlyAsync<DeviceResetterErroredOutException>()
                    .WithTimeoutInMs(100)
            ).WithInnerExceptionExactly<Exception>("native symbols not loaded blah blah");

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            eventsMonitor
                .Should().Raise(nameof(deviceResetter.StateChanged))
                .WithSender(deviceResetter)
                .WithArgs<StateChangedEventArgs>(args => args.NewState == EDeviceResetterState.Failed);

            eventsMonitor.Should().NotRaise(nameof(deviceResetter.FatalErrorOccurred)); //10

            //00 we dont want to disconnect the device regardless of the outcome
            //10 we dont expect the fatalerroroccurred event to be triggered because in this particular case the error is so
            //   exotic that its not worth it to complicate matters so much 
        }

        private class MockedErroneousDueToMissingSymbolsNativeDeviceResetterProxySpy : MockedNativeDeviceResetterProxySpy
        {
            public MockedErroneousDueToMissingSymbolsNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override void BeginReset()
            {
                base.BeginReset();

                Thread.Sleep(100);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
        
        [Fact]
        public async Task ShouldThrowDeviceResetterErroredOutException_GivenBluetoothErrorDuringReset()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedErroneousDueToBluetoothNativeDeviceResetterProxySpy(new GenericNativeDeviceResetterCallbacksProxy_());
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

                Task.Run(() => //00
                {
                    Thread.Sleep(10);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);
                    
                    Thread.Sleep(20);
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Failed);
                    FatalErrorOccurredAdvertisement("bluetooth error blah blah");
                    
                    //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
                });
            }
        }
        
        [Fact]
        public async Task ShouldThrowTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedGreenButSlowNativeDeviceResetterProxySpy(new GenericNativeDeviceResetterCallbacksProxy_());
            var deviceResetter = new McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();
            
            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync(timeoutInMs: 100));

            // Assert
            await work.Should().ThrowAsync<TimeoutException>();

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

            public override void BeginReset()
            {
                base.BeginReset();

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Idle, newState: EDeviceResetterState.Resetting);

                    Task.Delay(1_000).GetAwaiter().GetResult();
                    StateChangedAdvertisement(oldState: EDeviceResetterState.Resetting, newState: EDeviceResetterState.Complete);
                });

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }

        private class MockedNativeDeviceResetterProxySpy : INativeDeviceResetterProxy
        {
            private readonly INativeDeviceResetterCallbacksProxy _resetterCallbacksProxy;

            public bool DisconnectCalled { get; private set; }
            public bool BeginResetCalled { get; private set; }

            public object State { get; private set; }

            public string LastFatalErrorMessage => "";

            public IDeviceResetterEventEmitters DeviceResetter //keep this to conform to the interface
            {
                get => _resetterCallbacksProxy?.DeviceResetter;
                set
                {
                    if (_resetterCallbacksProxy == null)
                        return;

                    _resetterCallbacksProxy.DeviceResetter = value;
                }
            }

            protected MockedNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy)
            {
                _resetterCallbacksProxy = resetterCallbacksProxy;
            }

            public virtual void BeginReset()
            {
                BeginResetCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _resetterCallbacksProxy.LogMessageAdvertisement(message, category, level); //raises the actual event

            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
            {
                State = newState;
                _resetterCallbacksProxy.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event
            }

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => _resetterCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage); //raises the actual event
        }
    }
}