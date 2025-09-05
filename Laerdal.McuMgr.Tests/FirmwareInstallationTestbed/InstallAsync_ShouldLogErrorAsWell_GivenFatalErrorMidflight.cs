using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstallation;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed;

public partial class FirmwareInstallerTestbed
{
    [Fact]
    public async Task InstallAsync_ShouldLogErrorAsWell_GivenFatalErrorMidflight()
    {
        // Arrange
        var allLogEas = new List<LogEmittedEventArgs>(8);
        var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy20(new FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy());
        var firmwareInstaller = new FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

        using var eventsMonitor = firmwareInstaller.Monitor();

        // Act
        var work = new Func<Task>(() =>
        {
            firmwareInstaller.LogEmitted += (object _, in LogEmittedEventArgs ea) => allLogEas.Add(ea);
            
            return firmwareInstaller.InstallAsync(
                data: [1, 2, 3],
                maxTriesCount: 1,
                hostDeviceModel: "foobar",
                hostDeviceManufacturer: "acme corp."
            );
        });

        // Assert
        await work.Should().ThrowWithinAsync<AllFirmwareInstallationAttemptsFailedException>(3_000.Milliseconds());

        mockedNativeFirmwareInstallerProxy.CancelCalled.Should().BeFalse();
        mockedNativeFirmwareInstallerProxy.DisconnectCalled.Should().BeFalse(); //00
        mockedNativeFirmwareInstallerProxy.BeginInstallationCalled.Should().BeTrue();

        eventsMonitor.Should().NotRaise(nameof(firmwareInstaller.Cancelled));

        eventsMonitor
            .Should().Raise(nameof(firmwareInstaller.FatalErrorOccurred))
            .WithSender(firmwareInstaller);

        // eventsMonitor
        //     .OccurredEvents
        //     .Where(x => x.EventName == nameof(deviceResetter.LogEmitted))
        //     .SelectMany(x => x.Parameters)
        //     .OfType<LogEmittedEventArgs>() //xunit or fluent-assertions has memory corruption issues with this probably because of the zero-copy delegate! :(

        allLogEas
            .Count(l => l is {Level: ELogLevel.Error} && l.Message.Contains("blah blah", StringComparison.InvariantCulture))
            .Should()
            .BeGreaterThanOrEqualTo(1);

        //00 we dont want to disconnect the device regardless of the outcome
    }

    private class MockedGreenNativeFirmwareInstallerProxySpy20 : MockedNativeFirmwareInstallerProxySpy
    {
        public MockedGreenNativeFirmwareInstallerProxySpy20(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy) : base(firmwareInstallerCallbacksProxy)
        {
        }

        public override EFirmwareInstallationVerdict NativeBeginInstallation(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null,
            int? windowCapacity = null,
            int? memoryAlignment = null,
            int? pipelineDepth = null,
            int? byteAlignment = null
        )
        {
            base.NativeBeginInstallation(
                data: data,
                mode: mode,
                eraseSettings: eraseSettings,
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                initialMtuSize: initialMtuSize,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment,
                estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
            );

            StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.None);
            StateChangedAdvertisement(oldState: EFirmwareInstallationState.None, newState: EFirmwareInstallationState.Idle);
            
            Task.Run(async () => //00 vital
            {
                StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Validating);
                await Task.Delay(100);

                StateChangedAdvertisement(EFirmwareInstallationState.Validating, EFirmwareInstallationState.Error);
                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut, "blah blah", EGlobalErrorCode.Generic);
            });

            return EFirmwareInstallationVerdict.Success;

            //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
        }
    }
}