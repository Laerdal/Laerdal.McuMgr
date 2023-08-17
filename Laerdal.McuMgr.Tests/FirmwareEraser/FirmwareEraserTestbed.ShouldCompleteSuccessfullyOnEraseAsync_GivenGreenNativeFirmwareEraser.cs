using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Xunit;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public partial class FirmwareEraserTestbed
    {
        [Fact]
        public async Task ShouldCompleteSuccessfullyOnEraseAsync_GivenGreenNativeFirmwareEraser()
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

            public override void BeginErasure(int imageIndex)
            {
                base.BeginErasure(imageIndex);

                Task.Run(async () => //00 vital
                {
                    await Task.Delay(10);
                    StateChangedAdvertisement(EFirmwareErasureState.Idle, EFirmwareErasureState.Erasing);

                    await Task.Delay(20);
                    StateChangedAdvertisement(EFirmwareErasureState.Erasing, EFirmwareErasureState.Complete);
                });

                //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native eraser
            }
        }
    }
}