// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
        private readonly IOSFirmwareInstaller _iosFirmwareInstallerProxy;

        public FirmwareInstaller(CBPeripheral bluetoothDevice)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            _iosFirmwareInstallerProxy = new IOSFirmwareInstaller(
                listener: new IOSFirmwareInstallerListenerProxy(this),
                cbPeripheral: bluetoothDevice
            );
        }

        public EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? windowCapacity = null, //not applicable in ios
            int? memoryAlignment = null, //not applicable in ios
            int? pipelineDepth = null,
            int? byteAlignment = null
        )
        {
            //todo   nsdata should be tracked in a private variable and then cleaned up properly   currently it might get disposed ahead of time
            var nsData = NSData.FromArray(data);

            var verdict = _iosFirmwareInstallerProxy.BeginInstallation(
                mode: TranslateFirmwareInstallationMode(mode),
                imageData: nsData,
                eraseSettings: eraseSettings ?? false,
                // windowCapacity: not applicable in ios,
                // memoryAlignment: not applicable in ios,
                pipelineDepth: pipelineDepth ?? -1,
                byteAlignment: byteAlignment ?? -1,
                estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds ?? -1
            );

            return TranslateFirmwareInstallationVerdict(verdict);
        }

        public string LastFatalErrorMessage => _iosFirmwareInstallerProxy?.LastFatalErrorMessage;

        public void Cancel() => _iosFirmwareInstallerProxy.Cancel();
        public void Disconnect() => _iosFirmwareInstallerProxy.Disconnect();

        static private EIOSFirmwareInstallationMode TranslateFirmwareInstallationMode(EFirmwareInstallationMode mode) => mode switch
        {
            EFirmwareInstallationMode.TestOnly => EIOSFirmwareInstallationMode.TestOnly, //0
            EFirmwareInstallationMode.ConfirmOnly => EIOSFirmwareInstallationMode.ConfirmOnly,
            EFirmwareInstallationMode.TestAndConfirm => EIOSFirmwareInstallationMode.TestAndConfirm,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)

            //0 we have to separate enums
            //
            //  - EFirmwareInstallationMode which is publicly exposed and used by both android and ios
            //  - EIOSFirmwareInstallationMode which is specific to ios and should not be used by the api surface or the end users  
        };

        static private EFirmwareInstallationVerdict TranslateFirmwareInstallationVerdict(EIOSFirmwareInstallationVerdict verdict) => verdict switch
        {
            EIOSFirmwareInstallationVerdict.Success => EFirmwareInstallationVerdict.Success, //0
            EIOSFirmwareInstallationVerdict.FailedDeploymentError => EFirmwareInstallationVerdict.FailedDeploymentError,
            EIOSFirmwareInstallationVerdict.FailedInvalidSettings => EFirmwareInstallationVerdict.FailedInvalidSettings,
            EIOSFirmwareInstallationVerdict.FailedInvalidDataFile => EFirmwareInstallationVerdict.FailedInvalidDataFile,
            EIOSFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress => EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress,
            _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)

            //0 we have to separate enums
            //
            //  - EFirmwareInstallationVerdict which is publicly exposed and used by both android and ios
            //  - EIOSFirmwareInstallationVerdict which is specific to ios and should not be used by the api surface or the end users
        };

        // ReSharper disable once InconsistentNaming
        private sealed class IOSFirmwareInstallerListenerProxy : IOSListenerForFirmwareInstaller
        {
            private readonly FirmwareInstaller _installer;

            internal IOSFirmwareInstallerListenerProxy(FirmwareInstaller installer)
            {
                _installer = installer ?? throw new ArgumentNullException(nameof(installer));
            }

            public override void CancelledAdvertisement() => _installer.OnCancelled(new CancelledEventArgs());
            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _installer.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));
            public override void FatalErrorOccurredAdvertisement(string errorMessage) => _installer.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));

            public override void LogMessageAdvertisement(string message, string category, string level) => _installer.OnLogEmitted(new LogEmittedEventArgs(
                level: HelpersIOS.TranslateEIOSLogLevel(level),
                message: message,
                category: category,
                resource: "firmware-installer"
            ));
            
            public override void StateChangedAdvertisement(EIOSFirmwareInstallationState oldState, EIOSFirmwareInstallationState newState) => _installer.OnStateChanged(new StateChangedEventArgs(
                newState: TranslateEIOSFirmwareInstallationState(newState),
                oldState: TranslateEIOSFirmwareInstallationState(oldState)
            ));

            public override void FirmwareUploadProgressPercentageAndThroughputDataChangedAdvertisement(nint progressPercentage, float averageThroughput) => _installer.OnFirmwareUploadProgressPercentageAndThroughputDataChangedAdvertisement(new FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(
                averageThroughput: averageThroughput,
                progressPercentage: (int)progressPercentage
            ));

            // ReSharper disable once InconsistentNaming
            static private EFirmwareInstallationState TranslateEIOSFirmwareInstallationState(EIOSFirmwareInstallationState state) => state switch
            {
                EIOSFirmwareInstallationState.None => EFirmwareInstallationState.None,
                EIOSFirmwareInstallationState.Idle => EFirmwareInstallationState.Idle,
                EIOSFirmwareInstallationState.Error => EFirmwareInstallationState.Error,
                EIOSFirmwareInstallationState.Paused => EFirmwareInstallationState.Paused,
                EIOSFirmwareInstallationState.Testing => EFirmwareInstallationState.Testing,
                EIOSFirmwareInstallationState.Complete => EFirmwareInstallationState.Complete,
                EIOSFirmwareInstallationState.Uploading => EFirmwareInstallationState.Uploading,
                EIOSFirmwareInstallationState.Resetting => EFirmwareInstallationState.Resetting,
                EIOSFirmwareInstallationState.Cancelled => EFirmwareInstallationState.Cancelled,
                EIOSFirmwareInstallationState.Validating => EFirmwareInstallationState.Validating,
                EIOSFirmwareInstallationState.Confirming => EFirmwareInstallationState.Confirming,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}