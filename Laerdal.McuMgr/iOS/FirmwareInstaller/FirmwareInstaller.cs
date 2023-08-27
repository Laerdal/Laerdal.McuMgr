// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
        public FirmwareInstaller(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }

        static private INativeFirmwareInstallerProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeFirmwareInstallerProxy(
                bluetoothDevice: bluetoothDevice,
                nativeFirmwareInstallerCallbacksProxy: new GenericNativeFirmwareInstallerCallbacksProxy()
            );
        }

        // ReSharper disable once InconsistentNaming
        private sealed class IOSNativeFirmwareInstallerProxy : IOSListenerForFirmwareInstaller, INativeFirmwareInstallerProxy
        {
            private readonly IOSFirmwareInstaller _nativeIosFirmwareInstaller;
            private readonly INativeFirmwareInstallerCallbacksProxy _nativeFirmwareInstallerCallbacksProxy;

            public string Nickname { get; set; }
            
            public IFirmwareInstallerEventEmittable FirmwareInstaller { get; set; }

            internal IOSNativeFirmwareInstallerProxy(CBPeripheral bluetoothDevice, INativeFirmwareInstallerCallbacksProxy nativeFirmwareInstallerCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFirmwareInstallerCallbacksProxy = nativeFirmwareInstallerCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareInstallerCallbacksProxy));

                _nativeIosFirmwareInstaller = new IOSFirmwareInstaller(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFirmwareInstallerCallbacksProxy = nativeFirmwareInstallerCallbacksProxy; //composition-over-inheritance
            }



            #region commands

            public string LastFatalErrorMessage => _nativeIosFirmwareInstaller?.LastFatalErrorMessage;

            public void Cancel() => _nativeIosFirmwareInstaller?.Cancel();
            public void Disconnect() => _nativeIosFirmwareInstaller?.Disconnect();

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

                var verdict = _nativeIosFirmwareInstaller.BeginInstallation(
                    mode: TranslateFirmwareInstallationMode(mode),
                    imageData: nsData,
                    eraseSettings: eraseSettings ?? false,
                    pipelineDepth: pipelineDepth ?? -1,
                    byteAlignment: byteAlignment ?? -1,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds ?? -1
                );

                return TranslateFirmwareInstallationVerdict(verdict);
            }

            #endregion commands



            #region ios listener callbacks -> csharp event emitters
            
            public override void CancelledAdvertisement() => _nativeFirmwareInstallerCallbacksProxy?.CancelledAdvertisement();
            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _nativeFirmwareInstallerCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);

            public override void FatalErrorOccurredAdvertisement(EIOSFirmwareInstallationState state, EIOSFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage)
                => FatalErrorOccurredAdvertisement(
                    state: TranslateEIOSFirmwareInstallationState(state),
                    errorMessage: errorMessage,
                    fatalErrorType: TranslateEIOSFirmwareInstallerFatalErrorType(fatalErrorType)
                );
            
            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage) //just to conform to the interface
                => _nativeFirmwareInstallerCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    state: state,
                    errorMessage: errorMessage,
                    fatalErrorType: fatalErrorType
                );

            public override void LogMessageAdvertisement(string message, string category, string level)
                => LogMessageAdvertisement(
                    level: HelpersIOS.TranslateEIOSLogLevel(level),
                    message: message,
                    category: category,
                    resource: Nickname
                );

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource) //just to conform to the interface
                => _nativeFirmwareInstallerCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                );

            public override void StateChangedAdvertisement(EIOSFirmwareInstallationState oldState, EIOSFirmwareInstallationState newState)
                => StateChangedAdvertisement(
                    newState: TranslateEIOSFirmwareInstallationState(newState),
                    oldState: TranslateEIOSFirmwareInstallationState(oldState)
                );

            public void StateChangedAdvertisement(EFirmwareInstallationState oldState, EFirmwareInstallationState newState) //just to conform to the interface
                => _nativeFirmwareInstallerCallbacksProxy?.StateChangedAdvertisement(
                    newState: newState,
                    oldState: oldState
                );

            public override void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(nint progressPercentage, float averageThroughput)
                => FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement((int)progressPercentage, averageThroughput);
            
            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput) //just to conform to the interface
                => _nativeFirmwareInstallerCallbacksProxy?.FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                );
            
            #endregion


            static private EFirmwareInstallerFatalErrorType TranslateEIOSFirmwareInstallerFatalErrorType(EIOSFirmwareInstallerFatalErrorType fatalErrorType)
            {
                return fatalErrorType switch
                {
                    EIOSFirmwareInstallerFatalErrorType.Generic => EFirmwareInstallerFatalErrorType.Generic,
                    EIOSFirmwareInstallerFatalErrorType.InvalidFirmware => EFirmwareInstallerFatalErrorType.InvalidFirmware,
                    EIOSFirmwareInstallerFatalErrorType.InvalidSettings => EFirmwareInstallerFatalErrorType.InvalidSettings,
                    EIOSFirmwareInstallerFatalErrorType.DeploymentFailed => EFirmwareInstallerFatalErrorType.DeploymentFailed,
                    EIOSFirmwareInstallerFatalErrorType.FirmwareImageSwapTimeout => EFirmwareInstallerFatalErrorType.FirmwareImageSwapTimeout,
                    EIOSFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut => EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut,
                    _ => throw new ArgumentOutOfRangeException(nameof(fatalErrorType), actualValue: fatalErrorType, message: "Unknown enum value")
                };
            }

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
                EIOSFirmwareInstallationState.Cancelling => EFirmwareInstallationState.Cancelling,
                EIOSFirmwareInstallationState.Validating => EFirmwareInstallationState.Validating,
                EIOSFirmwareInstallationState.Confirming => EFirmwareInstallationState.Confirming,
                _ => throw new ArgumentOutOfRangeException(nameof(state), actualValue: state, message: "Unknown enum value")
            };

            static private EIOSFirmwareInstallationMode TranslateFirmwareInstallationMode(EFirmwareInstallationMode mode) => mode switch
            {
                EFirmwareInstallationMode.TestOnly => EIOSFirmwareInstallationMode.TestOnly, //0
                EFirmwareInstallationMode.ConfirmOnly => EIOSFirmwareInstallationMode.ConfirmOnly,
                EFirmwareInstallationMode.TestAndConfirm => EIOSFirmwareInstallationMode.TestAndConfirm,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown enum value")

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
                EIOSFirmwareInstallationVerdict.FailedInvalidFirmware => EFirmwareInstallationVerdict.FailedInvalidFirmware,
                EIOSFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress => EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress,
                _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value")

                //0 we have to separate enums
                //
                //  - EFirmwareInstallationVerdict which is publicly exposed and used by both android and ios
                //  - EIOSFirmwareInstallationVerdict which is specific to ios and should not be used by the api surface or the end users
            };
        }
    }
}