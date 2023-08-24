// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
        private readonly AndroidFirmwareInstallerProxy _androidFirmwareInstallerProxy;

        public FirmwareInstaller(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            _androidFirmwareInstallerProxy = new AndroidFirmwareInstallerProxy(this, androidContext, bluetoothDevice);
        }

        // public string LastFatalErrorMessage => _androidFirmwareInstallerProxy?.LastFatalErrorMessage;
        //
        // public EFirmwareInstallationVerdict BeginInstallation(
        //     byte[] data,
        //     EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
        //     bool? eraseSettings = null,
        //     int? estimatedSwapTimeInMilliseconds = null,
        //     int? windowCapacity = null, //   android only    not applicable for ios
        //     int? memoryAlignment = null, //  android only    not applicable for ios
        //     int? pipelineDepth = null, //    ios only        not applicable for android
        //     int? byteAlignment = null //     ios only        not applicable for android
        // )
        // {
        //     if (data == null || !data.Any())
        //         throw new ArgumentException("The data byte-array parameter is null or empty", nameof(data));
        //
        //     _androidFirmwareInstallerProxy.Nickname = "Firmware Installation"; //todo  get this from a parameter 
        //     var verdict = _androidFirmwareInstallerProxy.BeginInstallation(
        //         data: data,
        //         mode: TranslateFirmwareInstallationMode(mode),
        //         eraseSettings: eraseSettings ?? false,
        //         windowCapacity: windowCapacity ?? -1,
        //         memoryAlignment: memoryAlignment ?? -1,
        //         estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds ?? -1
        //         // pipelineDepth: not applicable in android
        //         // byteAlignment: not applicable in android
        //     );
        //
        //     return TranslateFirmwareInstallationVerdict(verdict);
        // }
        //
        // public void Cancel() => _androidFirmwareInstallerProxy.Cancel();
        // public void Disconnect() => _androidFirmwareInstallerProxy.Disconnect();

        static private EAndroidFirmwareInstallationMode TranslateFirmwareInstallationMode(EFirmwareInstallationMode mode) => mode switch
        {
            EFirmwareInstallationMode.TestOnly => EAndroidFirmwareInstallationMode.TestOnly, //0
            EFirmwareInstallationMode.ConfirmOnly => EAndroidFirmwareInstallationMode.ConfirmOnly,
            EFirmwareInstallationMode.TestAndConfirm => EAndroidFirmwareInstallationMode.TestAndConfirm,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)

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
                return EFirmwareInstallationVerdict.FailedInvalidDataFile;
            }
            
            if (verdict == EAndroidFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress)
            {
                return EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress;
            }

            throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null);

            //0 we have to separate enums
            //
            //  - EFirmwareInstallationVerdict which is publicly exposed and used by both android and ios
            //  - EAndroidFirmwareInstallationVerdict which is specific to android and should not be used by the api surface or the end users
        }

        private sealed class AndroidFirmwareInstallerProxy : AndroidFirmwareInstaller
        {
            public string Nickname { get; set; }
            
            private readonly FirmwareInstaller _installer;

            // ReSharper disable once UnusedMember.Local
            private AndroidFirmwareInstallerProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidFirmwareInstallerProxy(FirmwareInstaller installer, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _installer = installer ?? throw new ArgumentNullException(nameof(installer));
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage);

                _installer.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
            }
            
            public override void LogMessageAdvertisement(string message, string category, string level)
            {
                base.LogMessageAdvertisement(message, category, level);

                _installer.OnLogEmitted(new LogEmittedEventArgs(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category,
                    resource: Nickname
                ));
            }

            public override void CancelledAdvertisement()
            {
                base.CancelledAdvertisement(); //just in case
                
                _installer.OnCancelled(new CancelledEventArgs());
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case
                
                _installer.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));
            }

            public override void StateChangedAdvertisement(EAndroidFirmwareInstallationState oldState, EAndroidFirmwareInstallationState newState)
            {
                base.StateChangedAdvertisement(oldState, newState); //just in case

                _installer.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEAndroidFirmwareInstallationState(newState),
                    oldState: TranslateEAndroidFirmwareInstallationState(oldState)
                ));
            }

            public override void FirmwareUploadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput)
            {
                base.FirmwareUploadProgressPercentageAndThroughputDataChangedAdvertisement(progressPercentage, averageThroughput); //just in case

                _installer.OnFirmwareUploadProgressPercentageAndThroughputDataChanged(new FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
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
                
                if (state == EAndroidFirmwareInstallationState.Cancelled)
                {
                    return EFirmwareInstallationState.Cancelled;
                }
                
                if (state == EAndroidFirmwareInstallationState.Error)
                {
                    return EFirmwareInstallationState.Error;
                }

                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}