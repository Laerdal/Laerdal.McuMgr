using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task EraseAsync_ShouldCompleteSuccessfully_GivenGreenNativeFirmwareEraser()
        {
            // Arrange
            var mockedNativeFirmwareEraserProxy = new MockedGreenNativeFirmwareEraserProxySpy1(new McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy());
            var firmwareEraser = new McuMgr.FirmwareEraser.FirmwareEraser(mockedNativeFirmwareEraserProxy);

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