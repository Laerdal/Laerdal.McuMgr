using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.DeviceResetting;
using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetting.Contracts.Events;
using Laerdal.McuMgr.DeviceResetting.Contracts.Exceptions;
using Laerdal.McuMgr.DeviceResetting.Contracts.Native;

namespace Laerdal.McuMgr.Tests.DeviceResettingTestbed
{
    public partial class DeviceResetterTestbed
    {
        [Fact]
        public async Task ResetAsync_ShouldThrowDeviceResetterInternalErrorException_GivenErroneousDueToMissingNativeSymbolsNativeDeviceResetterProxy()
        {
            // Arrange
            var mockedNativeDeviceResetterProxy = new MockedErroneousDueToMissingSymbolsNativeDeviceResetterProxySpy(new DeviceResetter.GenericNativeDeviceResetterCallbacksProxy());
            var deviceResetter = new DeviceResetter(mockedNativeDeviceResetterProxy);
            using var eventsMonitor = deviceResetter.Monitor();

            // Act
            var work = new Func<Task>(() => deviceResetter.ResetAsync());

            // Assert
            (await work
                    .Should()
                    .ThrowWithinAsync<DeviceResetterInternalErrorException>(500.Milliseconds())
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

            public override EDeviceResetterInitializationVerdict BeginReset()
            {
                base.BeginReset();

                Thread.Sleep(100);

                throw new Exception("native symbols not loaded blah blah");
            }
        }
    }
}