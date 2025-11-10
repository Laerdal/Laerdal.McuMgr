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
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public FileDownloader(object nativeBluetoothDevice, object androidContext = null) : this( // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            androidContext: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<Context>(obj: androidContext, parameterName: nameof(androidContext), allowNulls: true),
            bluetoothDevice: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<BluetoothDevice>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice))
        )
        {
        }
        
        public FileDownloader(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        {
        }

        static private INativeFileDownloaderProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            return new AndroidFileDownloaderProxy(
                context: androidContext,
                bluetoothDevice: bluetoothDevice,
                fileDownloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy()
            );
        }

        private sealed class AndroidFileDownloaderProxy : AndroidFileDownloader, INativeFileDownloaderProxy 
        {
            private readonly INativeFileDownloaderCallbacksProxy _fileDownloaderCallbacksProxy;
            
            public IFileDownloaderEventEmittable FileDownloader //keep this to conform to the interface
            {
                get => _fileDownloaderCallbacksProxy!.FileDownloader;
                set => _fileDownloaderCallbacksProxy!.FileDownloader = value;
            }

            // ReSharper disable once UnusedMember.Local
            private AndroidFileDownloaderProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }
            
            internal AndroidFileDownloaderProxy(INativeFileDownloaderCallbacksProxy fileDownloaderCallbacksProxy, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _fileDownloaderCallbacksProxy = fileDownloaderCallbacksProxy ?? throw new ArgumentNullException(nameof(fileDownloaderCallbacksProxy));
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
            protected override void Dispose(bool disposing)
            {
                if (_alreadyDisposed)
                    return;

                if (!disposing)
                    return;
                
                TryCleanupInfrastructure();
                
                _alreadyDisposed = true;

                try
                {
                    base.Dispose(disposing: true);
                }
                catch
                {
                    //ignored
                }
            }
            
            private void TryCleanupInfrastructure()
            {
                try
                {
                    TryDisconnect();
                }
                catch
                {
                    //ignored
                }
            }

            #region commands
            
            // ReSharper disable once RedundantOverriddenMember
            public bool TrySetMinimumNativeLogLevel(ELogLevel minimumNativeLogLevel)
            {
                return base.TrySetMinimumNativeLogLevel((int) minimumNativeLogLevel);
            }
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryPause() //keep this override so as to amortize the native layer
            {
                return base.TryPause();
            }
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryResume() //keep this override so as to amortize the native layer
            {
                return base.TryResume();
            }
            
            // ReSharper disable once RedundantOverriddenMember
            // ReSharper disable once OptionalParameterHierarchyMismatch
            public override bool TryCancel(string reason = "") //keep this override so as to amortize the native layer
            {
                return base.TryCancel(reason);
            }
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryDisconnect() //keep this override so as to amortize the native layer
            {
                return base.TryDisconnect();
            }

            public EFileDownloaderVerdict NativeBeginDownload(
                string remoteFilePath,
                ELogLevel? minimumNativeLogLevel = null,
                int? initialMtuSize = null //  android only
            )
            {
                return TranslateFileDownloaderVerdict(base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize ?? -1,
                    minimumNativeLogLevelNumeric: (int) (minimumNativeLogLevel ?? ELogLevel.Error)
                ));
            }
            
            public bool TrySetContext(object context) //the parameter must be of type 'object' so that it wont cause problems in platforms other than android
            {
                var androidContext = context as Context ?? throw new ArgumentException($"Expected {nameof(Context)} to be an AndroidContext but got '{context?.GetType().Name ?? "null"}' instead", nameof(context));
                
                return base.TrySetContext(androidContext);
            }

            public bool TrySetBluetoothDevice(object bluetoothDevice)
            {
                var androidBluetoothDevice = bluetoothDevice as BluetoothDevice ?? throw new ArgumentException($"Expected {nameof(BluetoothDevice)} to be an AndroidBluetoothDevice but got '{bluetoothDevice?.GetType().Name ?? "null"}' instead", nameof(bluetoothDevice));
                
                return base.TrySetBluetoothDevice(androidBluetoothDevice);
            }
            
            public new bool TryInvalidateCachedInfrastructure()
            {
                return base.TryInvalidateCachedInfrastructure();
            }
            
            #endregion commands


            #region android callbacks -> csharp event emitters

            public override void FatalErrorOccurredAdvertisement(string resource, string errorMessage, int globalErrorCode)
            {
                base.FatalErrorOccurredAdvertisement(resource, errorMessage, globalErrorCode); //just in case

                FatalErrorOccurredAdvertisement(resource, errorMessage, (EGlobalErrorCode) globalErrorCode);
            }

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EGlobalErrorCode globalErrorCode)
            {
                _fileDownloaderCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    resource,
                    errorMessage,
                    globalErrorCode
                );
            }
            
            public override void LogMessageAdvertisement(string message, string category, string level, string resource)
            {
                base.LogMessageAdvertisement(message, category, level, resource);

                LogMessageAdvertisement(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category,
                    resource: resource //this is the remote-file-path essentially
                );
            }

            // keep this around to conform to the interface
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
            {
                _fileDownloaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource //essentially the remote filepath
                );
            }

            public override void CancelledAdvertisement(string reason)
            {
                base.CancelledAdvertisement(reason); //just in case

                _fileDownloaderCallbacksProxy?.CancelledAdvertisement(reason);
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case
                
                _fileDownloaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void StateChangedAdvertisement(
                string remoteFilePath,
                EAndroidFileDownloaderState oldState,
                EAndroidFileDownloaderState newState,
                long totalBytesToBeDownloaded,
                byte[] completeDownloadedData
            )
            {
                base.StateChangedAdvertisement(remoteFilePath, oldState, newState, totalBytesToBeDownloaded, completeDownloadedData); //just in case

                StateChangedAdvertisement(
                    oldState: TranslateEAndroidFileDownloaderState(oldState),
                    newState: TranslateEAndroidFileDownloaderState(newState),
                    remoteFilePath: remoteFilePath,
                    completeDownloadedData: completeDownloadedData,
                    totalBytesToBeDownloaded: totalBytesToBeDownloaded
                );
            }

            public void StateChangedAdvertisement(string remoteFilePath, EFileDownloaderState oldState, EFileDownloaderState newState, long totalBytesToBeDownloaded, byte[] completeDownloadedData)
            {
                _fileDownloaderCallbacksProxy?.StateChangedAdvertisement(
                    oldState: oldState,
                    newState: newState,
                    remoteFilePath: remoteFilePath,
                    completeDownloadedData: completeDownloadedData,
                    totalBytesToBeDownloaded: totalBytesToBeDownloaded
                );
            }

            public override void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
            {
                base.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, progressPercentage, currentThroughputInKBps, totalAverageThroughputInKBps); //just in case

                _fileDownloaderCallbacksProxy?.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
                    resourceId: resourceId,
                    progressPercentage: progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                );
            }
            
            #endregion android callbacks -> csharp event emitters -> helpers

            static private EFileDownloaderVerdict TranslateFileDownloaderVerdict(EAndroidFileDownloaderVerdict verdict)
            {
                if (verdict == EAndroidFileDownloaderVerdict.Success) //0
                {
                    return EFileDownloaderVerdict.Success;
                }
            
                if (verdict == EAndroidFileDownloaderVerdict.FailedInvalidSettings)
                {
                    return EFileDownloaderVerdict.FailedInvalidSettings;
                }

                if (verdict == EAndroidFileDownloaderVerdict.FailedErrorUponCommencing)
                {
                    return EFileDownloaderVerdict.FailedErrorUponCommencing;
                }

                if (verdict == EAndroidFileDownloaderVerdict.FailedDownloadAlreadyInProgress)
                {
                    return EFileDownloaderVerdict.FailedDownloadAlreadyInProgress;
                }

                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value");

                //0 we have to separate enums
                //
                //  - EFileDownloaderVerdict which is publicly exposed and used by both android and ios
                //  - EAndroidFileDownloaderVerdict which is specific to android and should not be used by the api surface or the end users
            }

            static private EFileDownloaderState TranslateEAndroidFileDownloaderState(EAndroidFileDownloaderState state)
            {
                if (state == EAndroidFileDownloaderState.None)
                {
                    return EFileDownloaderState.None;
                }
                
                if (state == EAndroidFileDownloaderState.Idle)
                {
                    return EFileDownloaderState.Idle;
                }

                if (state == EAndroidFileDownloaderState.Downloading)
                {
                    return EFileDownloaderState.Downloading;
                }

                if (state == EAndroidFileDownloaderState.Paused)
                {
                    return EFileDownloaderState.Paused;
                }
                
                if (state == EAndroidFileDownloaderState.Resuming)
                {
                    return EFileDownloaderState.Resuming;
                }

                if (state == EAndroidFileDownloaderState.Complete)
                {
                    return EFileDownloaderState.Complete;
                }
                
                if (state == EAndroidFileDownloaderState.Cancelled)
                {
                    return EFileDownloaderState.Cancelled;
                }
                
                if (state == EAndroidFileDownloaderState.Error)
                {
                    return EFileDownloaderState.Error;
                }

                if (state == EAndroidFileDownloaderState.Cancelling)
                {
                    return EFileDownloaderState.Cancelling;
                }

                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value");
            }
        }
    }
}