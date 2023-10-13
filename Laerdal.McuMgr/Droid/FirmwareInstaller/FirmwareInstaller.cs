// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Bindings.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
        public FirmwareInstaller(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        {
        }

        static private INativeFirmwareInstallerProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            return new AndroidFirmwareInstallerProxy(
                context: androidContext,
                bluetoothDevice: bluetoothDevice,
                firmwareInstallerCallbacksProxy: new GenericNativeFirmwareInstallerCallbacksProxy()
            );
        }

        private sealed class AndroidFirmwareInstallerProxy : AndroidFirmwareInstaller, INativeFirmwareInstallerProxy
        {
            public string Nickname { get; set; }

            private readonly INativeFirmwareInstallerCallbacksProxy _firmwareInstallerCallbacksProxy;
            
            public IFirmwareInstallerEventEmittable FirmwareInstaller //keep this to conform to the interface
            {
                get => _firmwareInstallerCallbacksProxy!.FirmwareInstaller;
                set => _firmwareInstallerCallbacksProxy!.FirmwareInstaller = value;
            }

            // ReSharper disable once UnusedMember.Local
            private AndroidFirmwareInstallerProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidFirmwareInstallerProxy(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _firmwareInstallerCallbacksProxy = firmwareInstallerCallbacksProxy ?? throw new ArgumentNullException(nameof(firmwareInstallerCallbacksProxy));
            }

            public EFirmwareInstallationVerdict BeginInstallation(
                byte[] data,
                EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
                bool? eraseSettings = null,
                int? estimatedSwapTimeInMilliseconds = null,
                int? windowCapacity = null,
                int? memoryAlignment = null,
                int? pipelineDepth = null, // ignored in android   it only affects ios
                int? byteAlignment = null //  ignored in android   it only affects ios
            )
            {
                var nativeVerdict = base.BeginInstallation(
                    data: data,
                    mode: TranslateFirmwareInstallationMode(mode),
                    eraseSettings: eraseSettings ?? false,
                    windowCapacity: windowCapacity ?? -1,
                    memoryAlignment: memoryAlignment ?? -1,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds ?? -1
                    // pipelineDepth: ignored in android   it only affects ios
                    // byteAlignment: ignored in android   it only affects ios
                );

                return TranslateFirmwareInstallationVerdict(nativeVerdict);
            }

            public override void FatalErrorOccurredAdvertisement(EAndroidFirmwareInstallationState state, EAndroidFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(state, fatalErrorType, errorMessage);

                FatalErrorOccurredAdvertisement(
                    state: TranslateEAndroidFirmwareInstallationState(state),
                    errorMessage: errorMessage,
                    fatalErrorType: TranslateEAndroidFirmwareInstallerFatalErrorType(fatalErrorType)
                );
            }

            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage) //just to conform to the interface
            {
                _firmwareInstallerCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    state: state,
                    errorMessage: errorMessage,
                    fatalErrorType: fatalErrorType
                );
            }
            
            public override void LogMessageAdvertisement(string message, string category, string level)
            {
                base.LogMessageAdvertisement(message, category, level);

                LogMessageAdvertisement(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category,
                    resource: Nickname
                );
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource) //just to conform to the interface
            {
                _firmwareInstallerCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                );
            }

            public override void CancelledAdvertisement()
            {
                base.CancelledAdvertisement(); //just in case

                _firmwareInstallerCallbacksProxy?.CancelledAdvertisement();
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case

                _firmwareInstallerCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void StateChangedAdvertisement(EAndroidFirmwareInstallationState oldState, EAndroidFirmwareInstallationState newState)
            {
                base.StateChangedAdvertisement(oldState, newState); //just in case

                StateChangedAdvertisement(
                    newState: TranslateEAndroidFirmwareInstallationState(newState),
                    oldState: TranslateEAndroidFirmwareInstallationState(oldState)
                );
            }

            public void StateChangedAdvertisement(EFirmwareInstallationState oldState, EFirmwareInstallationState newState) // just to conform to the interface
            {
                _firmwareInstallerCallbacksProxy?.StateChangedAdvertisement(newState: newState, oldState: oldState);
            }

            public override void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
            {
                base.FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput); //just in case

                _firmwareInstallerCallbacksProxy?.FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                );
            }

            static private EAndroidFirmwareInstallationMode TranslateFirmwareInstallationMode(EFirmwareInstallationMode mode) => mode switch
            {
                EFirmwareInstallationMode.TestOnly => EAndroidFirmwareInstallationMode.TestOnly, //0
                EFirmwareInstallationMode.ConfirmOnly => EAndroidFirmwareInstallationMode.ConfirmOnly,
                EFirmwareInstallationMode.TestAndConfirm => EAndroidFirmwareInstallationMode.TestAndConfirm,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown enum value")

                //0 we have to separate enums
                //
                //  - EFirmwareInstallationMode which is publicly exposed and used by both android and ios
                //  - EAndroidFirmwareInstallationMode which is specific to android and should not be used by the api surface or the end users  
            };

            static private EFirmwareInstallationVerdict TranslateFirmwareInstallationVerdict(EAndroidFirmwareInstallationVerdict verdict)
            {
                if (verdict == EAndroidFirmwareInstallationVerdict.Success) //0
                {
                    return EFirmwareInstallationVerdict.Success;
                }

                if (verdict == EAndroidFirmwareInstallationVerdict.FailedDeploymentError)
                {
                    return EFirmwareInstallationVerdict.FailedDeploymentError;
                }

                if (verdict == EAndroidFirmwareInstallationVerdict.FailedInvalidSettings)
                {
                    return EFirmwareInstallationVerdict.FailedInvalidSettings;
                }

                if (verdict == EAndroidFirmwareInstallationVerdict.FailedInvalidDataFile)
                {
                    return EFirmwareInstallationVerdict.FailedInvalidFirmware;
                }

                if (verdict == EAndroidFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress)
                {
                    return EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress;
                }

                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value");

                //0 we have to separate enums
                //
                //  - EFirmwareInstallationVerdict which is publicly exposed and used by both android and ios
                //  - EAndroidFirmwareInstallationVerdict which is specific to android and should not be used by the api surface or the end users
            }

            static private EFirmwareInstallerFatalErrorType TranslateEAndroidFirmwareInstallerFatalErrorType(EAndroidFirmwareInstallerFatalErrorType fatalErrorType)
            {
                if (fatalErrorType == EAndroidFirmwareInstallerFatalErrorType.Generic)
                {
                    return EFirmwareInstallerFatalErrorType.Generic;
                }
                
                if (fatalErrorType == EAndroidFirmwareInstallerFatalErrorType.InvalidSettings)
                {
                    return EFirmwareInstallerFatalErrorType.InvalidSettings;
                }
                
                if (fatalErrorType == EAndroidFirmwareInstallerFatalErrorType.InvalidFirmware)
                {
                    return EFirmwareInstallerFatalErrorType.InvalidFirmware;
                }
                
                if (fatalErrorType == EAndroidFirmwareInstallerFatalErrorType.DeploymentFailed)
                {
                    return EFirmwareInstallerFatalErrorType.DeploymentFailed;
                }
                
                if (fatalErrorType == EAndroidFirmwareInstallerFatalErrorType.FirmwareImageSwapTimeout)
                {
                    return EFirmwareInstallerFatalErrorType.FirmwareImageSwapTimeout;
                }
                
                if (fatalErrorType == EAndroidFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut)
                {
                    return EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut;
                }
                
                throw new ArgumentOutOfRangeException(nameof(fatalErrorType), fatalErrorType, "Unknown enum value");
            }

            static private EFirmwareInstallationState TranslateEAndroidFirmwareInstallationState(EAndroidFirmwareInstallationState state)
            {
                if (state == EAndroidFirmwareInstallationState.None)
                {
                    return EFirmwareInstallationState.None;
                }

                if (state == EAndroidFirmwareInstallationState.Idle)
                {
                    return EFirmwareInstallationState.Idle;
                }

                if (state == EAndroidFirmwareInstallationState.Validating)
                {
                    return EFirmwareInstallationState.Validating;
                }

                if (state == EAndroidFirmwareInstallationState.Uploading)
                {
                    return EFirmwareInstallationState.Uploading;
                }

                if (state == EAndroidFirmwareInstallationState.Paused)
                {
                    return EFirmwareInstallationState.Paused;
                }

                if (state == EAndroidFirmwareInstallationState.Testing)
                {
                    return EFirmwareInstallationState.Testing;
                }

                if (state == EAndroidFirmwareInstallationState.Confirming)
                {
                    return EFirmwareInstallationState.Confirming;
                }

                if (state == EAndroidFirmwareInstallationState.Resetting)
                {
                    return EFirmwareInstallationState.Resetting;
                }

                if (state == EAndroidFirmwareInstallationState.Complete)
                {
                    return EFirmwareInstallationState.Complete;
                }
                
                if (state == EAndroidFirmwareInstallationState.Cancelling)
                {
                    return EFirmwareInstallationState.Cancelling;
                }

                if (state == EAndroidFirmwareInstallationState.Cancelled)
                {
                    return EFirmwareInstallationState.Cancelled;
                }

                if (state == EAndroidFirmwareInstallationState.Error)
                {
                    return EFirmwareInstallationState.Error;
                }

                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value");
            }
        }
    }
}