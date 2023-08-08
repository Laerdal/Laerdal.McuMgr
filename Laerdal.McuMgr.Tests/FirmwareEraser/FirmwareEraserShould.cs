using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions;
using Xunit;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public class FirmwareEraserShould
    {
        private class MockedGreenNativeFirmwareEraserProxy : MockedNativeFirmwareEraserProxy
        {
            public MockedGreenNativeFirmwareEraserProxy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy) : base(eraserCallbacksProxy)
            {
            }

            public override void BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Task.Delay(10).GetAwaiter().GetResult();
                StateChangedAdvertisement(EFirmwareErasureState.Idle, EFirmwareErasureState.Erasing);

                Task.Delay(20).GetAwaiter().GetResult();
                StateChangedAdvertisement(EFirmwareErasureState.Erasing, EFirmwareErasureState.Complete);
            }
        }

        [Fact]
        public async Task ShouldCompleteSuccessfully_GivenGreenNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenNativeFirmwareEraserProxy(new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            await work.Should().CompleteWithinAsync(1.Seconds());

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedErroneousNativeFirmwareEraserProxy : MockedNativeFirmwareEraserProxy
        {
            public MockedErroneousNativeFirmwareEraserProxy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy) : base(eraserCallbacksProxy)
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
        public async Task ShouldErrorOut_GivenErroneousNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedErroneousNativeFirmwareEraserProxy(new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new Laerdal.McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            (await work.Should().ThrowAsync<FirmwareErasureErroredOutException>()).WithInnerExceptionExactly<Exception>("foobar");

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedNativeFirmwareEraserProxy : INativeFirmwareEraserProxy
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

            protected MockedNativeFirmwareEraserProxy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy)
            {
                _eraserCallbacksProxy = eraserCallbacksProxy;
            }

            public virtual void BeginErasure(int imageIndex)
            {
                BeginErasureCalled = true;

                Task.Delay(10).GetAwaiter().GetResult();
                StateChangedAdvertisement(EFirmwareErasureState.Idle, EFirmwareErasureState.Erasing);

                Task.Delay(20).GetAwaiter().GetResult();
                StateChangedAdvertisement(EFirmwareErasureState.Erasing, EFirmwareErasureState.Complete);
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