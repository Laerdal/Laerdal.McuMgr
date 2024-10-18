// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Net.Mime;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Bindings.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Native;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
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
            
            // public new void Dispose() { ... }    dont   there is no need to override the base implementation

            private bool _alreadyDisposed;
            protected override void Dispose(bool disposing)
            {
                if (_alreadyDisposed)
                {
                    base.Dispose(disposing); //vital
                    return;
                }

                if (disposing)
                {                   
                    CleanupInfrastructure();
                }

                _alreadyDisposed = true;

                base.Dispose(disposing);
            }
            
            private void CleanupInfrastructure()
            {
                try
                {
                    Disconnect();
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

            public EFileUploaderVerdict BeginUpload(
                string remoteFilePath,
                byte[] data,
                int? pipelineDepth,
                int? byteAlignment,
                int? initialMtuSize,
                int? windowCapacity,
                int? memoryAlignment
            )
            {
                return TranslateFileUploaderVerdict(base.BeginUpload(
                    data: data,
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize ?? -1,
                    windowCapacity: windowCapacity ?? -1,
                    memoryAlignment: memoryAlignment ?? -1
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
            
            public new bool TryInvalidateCachedTransport()
            {
                return base.TryInvalidateCachedTransport();
            }

            #endregion commands



            #region android callbacks -> csharp event emitters

            public override void FatalErrorOccurredAdvertisement(string resource, string errorMessage, int mcuMgrErrorCode, int fileUploaderGroupReturnCode)
            {
                base.FatalErrorOccurredAdvertisement(resource, errorMessage, mcuMgrErrorCode, fileUploaderGroupReturnCode); //just in case

                FatalErrorOccurredAdvertisement(resource, errorMessage, (EMcuMgrErrorCode) mcuMgrErrorCode, (EFileUploaderGroupReturnCode) fileUploaderGroupReturnCode);
            }

            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EMcuMgrErrorCode mcuMgrErrorCode, EFileUploaderGroupReturnCode fileUploaderGroupReturnCode)
            {
                _fileUploaderCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    resource,
                    errorMessage,
                    mcuMgrErrorCode,
                    fileUploaderGroupReturnCode
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
                _fileUploaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource //essentially the remote filepath
                );
            }

            public override void CancelledAdvertisement()
            {
                base.CancelledAdvertisement(); //just in case
                
                _fileUploaderCallbacksProxy?.CancelledAdvertisement();
            }

            public override void FileUploadedAdvertisement(string resource)
            {
                base.FileUploadedAdvertisement(resource); //just in case

                _fileUploaderCallbacksProxy?.FileUploadedAdvertisement(resource);
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case
                
                _fileUploaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void StateChangedAdvertisement(string resource, EAndroidFileUploaderState oldState, EAndroidFileUploaderState newState) 
            {
                base.StateChangedAdvertisement(resource, oldState, newState); //just in case

                StateChangedAdvertisement(
                    resource: resource, //essentially the remote filepath
                    oldState: TranslateEAndroidFileUploaderState(oldState),
                    newState: TranslateEAndroidFileUploaderState(newState)
                );
            }

            public void StateChangedAdvertisement(string resource, EFileUploaderState oldState, EFileUploaderState newState)
            {
                _fileUploaderCallbacksProxy?.StateChangedAdvertisement(
                    resource: resource,
                    oldState: oldState,
                    newState: newState
                );
            }

            public override void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
            {
                base.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput); //just in case

                _fileUploaderCallbacksProxy?.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
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