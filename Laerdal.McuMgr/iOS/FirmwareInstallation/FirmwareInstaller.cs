// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public FirmwareInstaller(object nativeBluetoothDevice) // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            : this(NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<CBPeripheral>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice)))
        {
        }

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
            private IOSFirmwareInstaller _nativeFirmwareInstaller;
            private readonly INativeFirmwareInstallerCallbacksProxy _nativeFirmwareInstallerCallbacksProxy;

            public string Nickname { get; set; }
            
            internal IOSNativeFirmwareInstallerProxy(CBPeripheral bluetoothDevice, INativeFirmwareInstallerCallbacksProxy nativeFirmwareInstallerCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFirmwareInstallerCallbacksProxy = nativeFirmwareInstallerCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareInstallerCallbacksProxy));

                _nativeFirmwareInstaller = new IOSFirmwareInstaller(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFirmwareInstallerCallbacksProxy = nativeFirmwareInstallerCallbacksProxy; //composition-over-inheritance
            }

            public new void Dispose()
            {
                Dispose(disposing: true); //doesnt throw

                try
                {
                    base.Dispose();
                }
                catch
                {
                    //ignored
                }
                
                GC.SuppressFinalize(this);
            }

            private bool _alreadyDisposed;
            private NSData _nsDataForFirmwareOfCurrentlyActiveInstallation;
            protected override void Dispose(bool disposing)
            {
                if (_alreadyDisposed)
                    return;

                if (!disposing)
                    return;
                
                TryCleanupInfrastructure();
                TryCleanupResourcesOfLastInstallation(); // shouldnt be necessary   but just in case

                _alreadyDisposed = true;
                
                base.Dispose(disposing: true);
            }

            private void TryCleanupInfrastructure()
            {
                try
                {
                    Disconnect();
                }
                catch
                {
                    // ignored
                }

                _nativeFirmwareInstaller?.Dispose();
                _nativeFirmwareInstaller = null;
            }

            public void TryCleanupResourcesOfLastInstallation() //00
            {
                try
                {
                    _nsDataForFirmwareOfCurrentlyActiveInstallation?.Dispose();
                    _nsDataForFirmwareOfCurrentlyActiveInstallation = null;
                }
                catch
                {
                    // ignored
                }
                
                //00 the method needs to be public so that it can be called manually when someone uses BeginUpload() instead of UploadAsync()!
            }

            #region commands

            public string LastFatalErrorMessage => _nativeFirmwareInstaller?.LastFatalErrorMessage;

            public void Cancel() => _nativeFirmwareInstaller?.Cancel();
            public void Disconnect() => _nativeFirmwareInstaller?.Disconnect();

            public EFirmwareInstallationVerdict NativeBeginInstallation(
                byte[] data,
                EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
                bool? eraseSettings = null,
                int? estimatedSwapTimeInMilliseconds = null,
                int? initialMtuSize = null,

                int? windowCapacity = null, //  not applicable in ios
                int? memoryAlignment = null, // not applicable in ios
                int? pipelineDepth = null,
                int? byteAlignment = null
            )
            {
                if (_nativeFirmwareInstaller == null)
                    throw new InvalidOperationException("The native firmware installer is not initialized");
                
                var nsDataOfFirmware = NSData.FromArray(data);
                
                var verdict = TranslateFirmwareInstallationVerdict(_nativeFirmwareInstaller.BeginInstallation(
                    mode: TranslateFirmwareInstallationMode(mode),
                    imageData: nsDataOfFirmware,
                    eraseSettings: eraseSettings ?? false,
                    
                    pipelineDepth: pipelineDepth ?? -1,
                    byteAlignment: byteAlignment ?? -1,
                    initialMtuSize: initialMtuSize ?? -1,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds ?? -1
                ));

                if (verdict != EFirmwareInstallationVerdict.Success)
                {
                    nsDataOfFirmware.Dispose();
                }
                else
                {
                    _nsDataForFirmwareOfCurrentlyActiveInstallation = nsDataOfFirmware;
                }

                return verdict;
            }

            #endregion commands



            #region native callbacks -> csharp events
            
            public IFirmwareInstallerEventEmittable FirmwareInstaller
            {
                get => _nativeFirmwareInstallerCallbacksProxy!.FirmwareInstaller;
                set => _nativeFirmwareInstallerCallbacksProxy!.FirmwareInstaller = value;
            }
            
            public override void CancelledAdvertisement() => _nativeFirmwareInstallerCallbacksProxy?.CancelledAdvertisement();
            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _nativeFirmwareInstallerCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);

            public override void FatalErrorOccurredAdvertisement(EIOSFirmwareInstallationState state, EIOSFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, nint globalErrorCode)
                => FatalErrorOccurredAdvertisement(
                    state: TranslateEIOSFirmwareInstallationState(state),
                    errorMessage: errorMessage,
                    fatalErrorType: TranslateEIOSFirmwareInstallerFatalErrorType(fatalErrorType),
                    globalErrorCode: (EGlobalErrorCode) globalErrorCode
                );

            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, EGlobalErrorCode globalErrorCode) //just to conform to the interface
                => _nativeFirmwareInstallerCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    state: state,
                    errorMessage: errorMessage,
                    fatalErrorType: fatalErrorType,
                    globalErrorCode: globalErrorCode
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

            public override void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(nint progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement((int)progressPercentage, currentThroughputInKBps, totalAverageThroughputInKBps);
            
            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps) //just to conform to the interface
                => _nativeFirmwareInstallerCallbacksProxy?.FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    progressPercentage: progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                );
            
            #endregion


            static private EFirmwareInstallerFatalErrorType TranslateEIOSFirmwareInstallerFatalErrorType(EIOSFirmwareInstallerFatalErrorType fatalErrorType)
            {
                return fatalErrorType switch
                {
                    EIOSFirmwareInstallerFatalErrorType.Generic => EFirmwareInstallerFatalErrorType.Generic,
                    EIOSFirmwareInstallerFatalErrorType.InvalidSettings => EFirmwareInstallerFatalErrorType.InvalidSettings,
                    EIOSFirmwareInstallerFatalErrorType.GivenFirmwareIsUnhealthy => EFirmwareInstallerFatalErrorType.GivenFirmwareDataUnhealthy,
                    EIOSFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut => EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut,
                    EIOSFirmwareInstallerFatalErrorType.InstallationAlreadyInProgress => EFirmwareInstallerFatalErrorType.InstallationAlreadyInProgress,
                    EIOSFirmwareInstallerFatalErrorType.InstallationInitializationFailed => EFirmwareInstallerFatalErrorType.InstallationInitializationFailed,
                    EIOSFirmwareInstallerFatalErrorType.FirmwareFinishingImageSwapTimeout => EFirmwareInstallerFatalErrorType.FirmwareFinishingImageSwapTimeout,
                    EIOSFirmwareInstallerFatalErrorType.PostInstallationDeviceRebootingFailed => EFirmwareInstallerFatalErrorType.PostInstallationDeviceRebootingFailed,
                    EIOSFirmwareInstallerFatalErrorType.FirmwareExtendedDataIntegrityChecksFailed => EFirmwareInstallerFatalErrorType.FirmwareExtendedDataIntegrityChecksFailed,
                    EIOSFirmwareInstallerFatalErrorType.FirmwarePostInstallationConfirmationFailed => EFirmwareInstallerFatalErrorType.FirmwarePostInstallationConfirmationFailed,
                    EIOSFirmwareInstallerFatalErrorType.PostInstallationDeviceHealthcheckTestsFailed => EFirmwareInstallerFatalErrorType.PostInstallationDeviceHealthcheckTestsFailed,
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
                EIOSFirmwareInstallationVerdict.FailedInvalidSettings => EFirmwareInstallationVerdict.FailedInvalidSettings,
                EIOSFirmwareInstallationVerdict.FailedGivenFirmwareUnhealthy => EFirmwareInstallationVerdict.FailedGivenFirmwareUnhealthy,
                EIOSFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress => EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress,
                EIOSFirmwareInstallationVerdict.FailedInstallationInitializationErroredOut => EFirmwareInstallationVerdict.FailedInstallationInitializationErroredOut,
                _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value")

                //0 we have to separate enums
                //
                //  - EFirmwareInstallationVerdict which is publicly exposed and used by both android and ios
                //  - EIOSFirmwareInstallationVerdict which is specific to ios and should not be used by the api surface or the end users
            };
        }
    }
}
