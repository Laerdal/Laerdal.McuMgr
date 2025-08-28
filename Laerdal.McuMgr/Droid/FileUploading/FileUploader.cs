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
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Native;

namespace Laerdal.McuMgr.FileUploading
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        public FileUploader(object nativeBluetoothDevice, object androidContext = null) : this( // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            androidContext: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<Context>(obj: androidContext, parameterName: nameof(androidContext), allowNulls: true),
            bluetoothDevice: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<BluetoothDevice>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice))
        )
        {
        }
        
        public FileUploader(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        {
        }

        static private INativeFileUploaderProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            return new AndroidFileUploaderProxy(
                context: androidContext,
                bluetoothDevice: bluetoothDevice,
                fileUploaderCallbacksProxy: new FileUploader.GenericNativeFileUploaderCallbacksProxy()
            );
        }

        private sealed class AndroidFileUploaderProxy : AndroidFileUploader, INativeFileUploaderProxy
        {
            private readonly INativeFileUploaderCallbacksProxy _fileUploaderCallbacksProxy;
            
            public IFileUploaderEventEmittable FileUploader //keep this to conform to the interface
            {
                get => _fileUploaderCallbacksProxy!.FileUploader;
                set => _fileUploaderCallbacksProxy!.FileUploader = value;
            }

            // ReSharper disable once UnusedMember.Local
            private AndroidFileUploaderProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }
            
            internal AndroidFileUploaderProxy(INativeFileUploaderCallbacksProxy fileUploaderCallbacksProxy, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _fileUploaderCallbacksProxy = fileUploaderCallbacksProxy ?? throw new ArgumentNullException(nameof(fileUploaderCallbacksProxy));
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
                
                CleanupInfrastructure();
                
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

            private void CleanupInfrastructure()
            {
                try
                {
                    // ReSharper disable once RedundantBaseQualifier
                    base.NativeDispose(); //java-glue-library method 
                }
                catch
                {
                    // ignored
                }
            }

            public void CleanupResourcesOfLastUpload()
            {
                //nothing to do in android
            }

            #region commands

            // ReSharper disable UnusedParameter.Local
            public EFileUploaderVerdict BeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                int? initialMtuSize, //both ios and android
                int? pipelineDepth, //   ios
                int? byteAlignment, //   ios
                int? windowCapacity, //  android
                int? memoryAlignment //  android
            ) // ReSharper enable UnusedParameter.Local
            {
                return TranslateFileUploaderVerdict(base.BeginUpload(
                    data: data,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize ?? -1,
                    windowCapacity: windowCapacity ?? -1,
                    memoryAlignment: memoryAlignment ?? -1
                ));
            }
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryPause()
            {
                return base.TryPause();
            }
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryResume()
            {
                return base.TryResume();
            }
            
            // ReSharper disable once RedundantOverriddenMember
            // ReSharper disable once OptionalParameterHierarchyMismatch
            public override bool TryCancel(string reason = "")
            {
                return base.TryCancel(reason);
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
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryDisconnect() //identical to base.TryDisconnect()
            {
                return base.TryDisconnect();
            }
            
            // ReSharper disable once RedundantOverriddenMember
            public override bool TryInvalidateCachedInfrastructure()
            {
                return base.TryInvalidateCachedInfrastructure();
            }

            #endregion commands



            #region android callbacks -> csharp event emitters

            public override void FatalErrorOccurredAdvertisement(string resourceId, string remoteFilePath, string errorMessage, int globalErrorCode)
            {
                base.FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, errorMessage, globalErrorCode); //just in case

                FatalErrorOccurredAdvertisement(resourceId, remoteFilePath, errorMessage, (EGlobalErrorCode) globalErrorCode);
            }

            public void FatalErrorOccurredAdvertisement(string resourceId, string remoteFilePath, string errorMessage, EGlobalErrorCode globalErrorCode)
            {
                _fileUploaderCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    errorMessage: errorMessage,
                    globalErrorCode: globalErrorCode
                );
            }
            
            public override void LogMessageAdvertisement(string message, string category, string level, string resourceId)
            {
                base.LogMessageAdvertisement(message, category, level, resourceId);

                LogMessageAdvertisement(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category,
                    resourceId: resourceId //this is the remote-file-path essentially
                );
            }

            // keep this around to conform to the interface
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resourceId)
            {
                _fileUploaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resourceId: resourceId //essentially the remote filepath
                );
            }
            
            public override void CancellingAdvertisement(string reason)
            {
                base.CancellingAdvertisement(reason); //just in case
                
                _fileUploaderCallbacksProxy?.CancellingAdvertisement(reason);
            }

            public override void CancelledAdvertisement(string reason)
            {
                base.CancelledAdvertisement(reason); //just in case
                
                _fileUploaderCallbacksProxy?.CancelledAdvertisement(reason);
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case
                
                _fileUploaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void StateChangedAdvertisement(string resourceId, string remoteFilePath, EAndroidFileUploaderState oldState, EAndroidFileUploaderState newState, long totalBytesToBeUploaded)
            {
                base.StateChangedAdvertisement(resourceId, remoteFilePath, oldState, newState, totalBytesToBeUploaded); //just in case

                StateChangedAdvertisement(
                    oldState: TranslateEAndroidFileUploaderState(oldState),
                    newState: TranslateEAndroidFileUploaderState(newState),
                    resourceId: resourceId, //essentially the remote filepath
                    remoteFilePath: remoteFilePath,
                    totalBytesToBeUploaded: totalBytesToBeUploaded
                );
            }

            public void StateChangedAdvertisement(string resourceId, string remoteFilePath, EFileUploaderState oldState, EFileUploaderState newState, long totalBytesToBeUploaded) //conforms to the interface
            {
                _fileUploaderCallbacksProxy?.StateChangedAdvertisement(
                    oldState: oldState,
                    newState: newState,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    totalBytesToBeUploaded: totalBytesToBeUploaded
                );
            }

            public override void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, string remoteFilePath, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
            {
                base.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId, remoteFilePath, progressPercentage, currentThroughputInKBps, totalAverageThroughputInKBps); //just in case

                _fileUploaderCallbacksProxy?.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    progressPercentage: progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                );
            }
            
            #endregion android callbacks -> csharp event emitters -> helpers

            
            static private EFileUploaderVerdict TranslateFileUploaderVerdict(EAndroidFileUploaderVerdict verdict)
            {
                if (verdict == EAndroidFileUploaderVerdict.Success) //0
                {
                    return EFileUploaderVerdict.Success;
                }
                
                if (verdict == EAndroidFileUploaderVerdict.FailedInvalidData)
                {
                    return EFileUploaderVerdict.FailedInvalidData;
                }
            
                if (verdict == EAndroidFileUploaderVerdict.FailedInvalidSettings)
                {
                    return EFileUploaderVerdict.FailedInvalidSettings;
                }

                if (verdict == EAndroidFileUploaderVerdict.FailedErrorUponCommencing)
                {
                    return EFileUploaderVerdict.FailedErrorUponCommencing;
                }

                if (verdict == EAndroidFileUploaderVerdict.FailedOtherUploadAlreadyInProgress)
                {
                    return EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress;
                }

                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value");

                //0 we have to separate enums
                //
                //  - EFileUploaderVerdict which is publicly exposed and used by both android and ios
                //  - EAndroidFileUploaderVerdict which is specific to android and should not be used by the api surface or the end users
            }

            static private EFileUploaderState TranslateEAndroidFileUploaderState(EAndroidFileUploaderState state)
            {
                if (state == EAndroidFileUploaderState.None)
                {
                    return EFileUploaderState.None;
                }
                
                if (state == EAndroidFileUploaderState.Idle)
                {
                    return EFileUploaderState.Idle;
                }

                if (state == EAndroidFileUploaderState.Uploading)
                {
                    return EFileUploaderState.Uploading;
                }

                if (state == EAndroidFileUploaderState.Paused)
                {
                    return EFileUploaderState.Paused;
                }
                
                if (state == EAndroidFileUploaderState.Resuming)
                {
                    return EFileUploaderState.Resuming;
                }

                if (state == EAndroidFileUploaderState.Complete)
                {
                    return EFileUploaderState.Complete;
                }
                
                if (state == EAndroidFileUploaderState.Cancelled)
                {
                    return EFileUploaderState.Cancelled;
                }
                
                if (state == EAndroidFileUploaderState.Error)
                {
                    return EFileUploaderState.Error;
                }

                if (state == EAndroidFileUploaderState.Cancelling)
                {
                    return EFileUploaderState.Cancelling;
                }

                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value");
            }
        }
    }
}