using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareErasure;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareErasureTestbed
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task EraseAsync_ShouldCompleteSuccessfully_GivenGreenNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenNativeFirmwareEraserProxySpy1(new FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new FirmwareEraser(mockedNativeFirmwareEraserProxy);

            firmwareEraser.LogEmitted += (object _, in LogEmittedEventArgs _) => throw new Exception($"{nameof(firmwareEraser.LogEmitted)} -> oops!"); //library should be immune to any and all user-land exceptions 
            firmwareEraser.StateChanged += (_, _) => throw new Exception($"{nameof(firmwareEraser.StateChanged)} -> oops!");
            firmwareEraser.BusyStateChanged += (_, _) => throw new Exception($"{nameof(firmwareEraser.BusyStateChanged)} -> oops!");
            firmwareEraser.FatalErrorOccurred += (_, _) => throw new Exception($"{nameof(firmwareEraser.FatalErrorOccurred)} -> oops!");

            // Act
            var work = new Func<Task>(() => firmwareEraser.EraseAsync(imageIndex: 2));

            // Assert
            await work.Should().CompleteWithinAsync(5.Seconds());

            mockedNativeFirmwareEraserProxy.DisconnectCalled.Should().BeFalse(); //00
            mockedNativeFirmwareEraserProxy.BeginErasureCalled.Should().BeTrue();

            //00 we dont want to disconnect the device regardless of the outcome
        }

        private class MockedGreenNativeFirmwareEraserProxySpy1 : MockedNativeFirmwareEraserProxySpy
        {
            public MockedGreenNativeFirmwareEraserProxySpy1(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy) : base(eraserCallbacksProxy)
            {
            }

            public override EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(EFirmwareErasureState.Idle, EFirmwareErasureState.Erasing);

                    await Task.Delay(20);
                    StateChangedAdvertisement(EFirmwareErasureState.Erasing, EFirmwareErasureState.Complete);
                });
                
                return EFirmwareErasureInitializationVerdict.Success;

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native eraser
            }
        }
    }
}