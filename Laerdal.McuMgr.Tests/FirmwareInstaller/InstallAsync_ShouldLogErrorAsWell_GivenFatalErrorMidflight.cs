using FluentAssertions;
using FluentAssertions.Extensions;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller;

public partial class FirmwareInstallerTestbed
{
    [Fact]
    public async Task InstallAsync_ShouldLogErrorAsWell_GivenFatalErrorMidflight()
    {
        // Arrange
        var allLogEas = new List<LogEmittedEventArgs>(8);
        var mockedNativeFirmwareInstallerProxy = new MockedGreenNativeFirmwareInstallerProxySpy20(new McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy());
        var firmwareInstaller = new McuMgr.FirmwareInstaller.FirmwareInstaller(mockedNativeFirmwareInstallerProxy);

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

        public override EFirmwareInstallationVerdict BeginInstallation(
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
            var verdict = base.BeginInstallation(
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

            Task.Run(async () => //00 vital
            {
                StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Idle);
                await Task.Delay(100);

                StateChangedAdvertisement(EFirmwareInstallationState.Idle, EFirmwareInstallationState.Validating);
                await Task.Delay(100);

                StateChangedAdvertisement(EFirmwareInstallationState.Validating, EFirmwareInstallationState.Error);
                FatalErrorOccurredAdvertisement(EFirmwareInstallationState.Uploading, EFirmwareInstallerFatalErrorType.InvalidFirmware, "blah blah", EGlobalErrorCode.Generic);
            });

            return verdict;

            //00 simulating the state changes in a background thread is vital in order to simulate the async nature of the native uploader
        }
    }
}