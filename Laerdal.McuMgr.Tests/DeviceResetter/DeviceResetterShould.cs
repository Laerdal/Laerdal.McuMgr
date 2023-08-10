using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions;
using Xunit;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public class DeviceResetterShould
    {
        [Fact]
        public async Task ShouldCompleteSuccessfully_GivenGreenNativeDeviceResetter()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedGreenNativeDeviceResetterProxy(new McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            await work.Should().CompleteWithinAsync(0.5.Seconds());

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeDeviceResetterProxy : MockedNativeDeviceResetterProxy
        {
            public MockedGreenNativeDeviceResetterProxy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override void BeginReset()
            {
                base.BeginReset();

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(EDeviceResetterState.Idle, EDeviceResetterState.Resetting);

                    Task.Delay(20).GetAwaiter().GetResult();
                    StateChangedAdvertisement(EDeviceResetterState.Resetting, EDeviceResetterState.Complete);
                });
                
                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }

        [Fact]
        public async Task ShouldThrowDeviceResetterErroredOutException_GivenErroneousNativeDeviceResetter()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedErroneousNativeDeviceResetterProxy(new Laerdal.McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            (await work.Should().ThrowAsync<DeviceResetterErroredOutException>()).WithInnerExceptionExactly<Exception>("foobar");

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeDeviceResetterProxy : MockedNativeDeviceResetterProxy
        {
            public MockedErroneousNativeDeviceResetterProxy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy) : base(resetterCallbacksProxy)
            {
            }

            public override void BeginReset()
            {
                base.BeginReset();

                Thread.Sleep(100);

                throw new Exception("foobar");
            }
        }
        
        [Fact]
        public async Task ShouldThrowTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedGreenNativeDeviceResetterProxy(new Laerdal.McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new Laerdal.McuMgr.DeviceResetter.DeviceResetter(mockedNativeDeviceResetterProxy);

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync(timeoutInMs: 1));

            // Assert
            await work.Should().ThrowAsync<TimeoutException>();

            mockedNativeDeviceResetterProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeDeviceResetterProxy.BeginResetCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedNativeDeviceResetterProxy : INativeDeviceResetterProxy
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

            protected MockedNativeDeviceResetterProxy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy)
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