using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions;
using Xunit;
using GenericNativeFirmwareEraserCallbacksProxy_ = Laerdal.McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public class FirmwareEraserShould
    {
        [Fact]
        public async Task ShouldCompleteSuccessfully_GivenGreenNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenNativeFirmwareEraserProxySpy(new GenericNativeFirmwareEraserCallbacksProxy_());
            var firmwareEraser = new McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            await work.Should().CompleteWithinAsync(0.5.Seconds());

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareEraserProxySpy : MockedNativeFirmwareEraserProxySpy
        {
            public MockedGreenNativeFirmwareEraserProxySpy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy) : base(eraserCallbacksProxy)
            {
            }

            public override void BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(EFirmwareErasureState.Idle, EFirmwareErasureState.Erasing);

                    Task.Delay(20).GetAwaiter().GetResult();
                    StateChangedAdvertisement(EFirmwareErasureState.Erasing, EFirmwareErasureState.Complete);
                });

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native eraser
            }
        }

        [Fact]
        public async Task ShouldThrowFirmwareErasureErroredOutException_GivenErroneousNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedErroneousNativeFirmwareEraserProxySpy(new GenericNativeFirmwareEraserCallbacksProxy_());
            var firmwareEraser = new McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            (await work.Should().ThrowAsync<FirmwareErasureErroredOutException>()).WithInnerExceptionExactly<Exception>("foobar");

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFirmwareEraserProxySpy : MockedNativeFirmwareEraserProxySpy
        {
            public MockedErroneousNativeFirmwareEraserProxySpy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy) : base(eraserCallbacksProxy)
            {
            }

            public override void BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Thread.Sleep(100);

                throw new Exception("foobar");
            }
        }
        
        [Fact]
        public async Task ShouldThrowTimeoutException_GivenTooSmallTimeout()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenButSlowNativeFirmwareEraserProxySpy(new GenericNativeFirmwareEraserCallbacksProxy_());
            var firmwareEraser = new McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);
            using var eventsMonitor = firmwareEraser.Monitor();

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2, timeoutInMs: 100));

            // Assert
            await work.Should().ThrowAsync<TimeoutException>();

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

                Task.Run(() => //00 vital
                {
                    Task.Delay(10).GetAwaiter().GetResult();
                    StateChangedAdvertisement(oldState: EFirmwareErasureState.Idle, newState: EFirmwareErasureState.Erasing);

                    Task.Delay(1_000).GetAwaiter().GetResult();
                    StateChangedAdvertisement(oldState: EFirmwareErasureState.Erasing, newState: EFirmwareErasureState.Complete);
                });

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native resetter
            }
        }

        private class MockedNativeFirmwareEraserProxySpy : INativeFirmwareEraserProxy
        {
            private readonly INativeFirmwareEraserCallbacksProxy _eraserCallbacksProxy;

            public bool DisconnectCalled { get; private set; }
            public bool BeginErasureCalled { get; private set; }

            public string LastFatalErrorMessage => "";

            public IFirmwareEraserEventEmitters FirmwareEraser //keep this to conform to the interface
            {
                get => _eraserCallbacksProxy?.FirmwareEraser;
                set
                {
                    if (_eraserCallbacksProxy == null)
                        return;

                    _eraserCallbacksProxy.FirmwareEraser = value;
                }
            }

            protected MockedNativeFirmwareEraserProxySpy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy)
            {
                _eraserCallbacksProxy = eraserCallbacksProxy;
            }

            public virtual void BeginErasure(int imageIndex)
            {
                BeginErasureCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _eraserCallbacksProxy.LogMessageAdvertisement(message, category, level); //raises the actual event

            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState)
                => _eraserCallbacksProxy.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _eraserCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => _eraserCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage); //raises the actual event
        }
    }
}