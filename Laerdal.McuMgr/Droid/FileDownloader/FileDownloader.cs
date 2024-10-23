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
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
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

            #region commands

            public EFileDownloaderVerdict BeginDownload(
                string remoteFilePath,
                int? initialMtuSize = null //  android only
            )
            {
                return TranslateFileDownloaderVerdict(base.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize ?? -1
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

            public override void FatalErrorOccurredAdvertisement(string resource, string errorMessage, int mcuMgrErrorCode, int fileOperationGroupReturnCode)
            {
                base.FatalErrorOccurredAdvertisement(resource, errorMessage, mcuMgrErrorCode, fileOperationGroupReturnCode); //just in case

                FatalErrorOccurredAdvertisement(resource, errorMessage, (EMcuMgrErrorCode) mcuMgrErrorCode, (EFileOperationGroupReturnCode) fileOperationGroupReturnCode);
            }
            
            public void FatalErrorOccurredAdvertisement(string resource, string errorMessage, EMcuMgrErrorCode mcuMgrErrorCode, EFileOperationGroupReturnCode fileUploaderGroupReturnCode)
            {
                _fileDownloaderCallbacksProxy?.FatalErrorOccurredAdvertisement(
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
                _fileDownloaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource //essentially the remote filepath
                );
            }

            public override void CancelledAdvertisement()
            {
                base.CancelledAdvertisement(); //just in case
                
                _fileDownloaderCallbacksProxy?.CancelledAdvertisement();
            }

            public override void DownloadCompletedAdvertisement(string resource, byte[] data)
            {
                base.DownloadCompletedAdvertisement(resource, data); //just in case

                _fileDownloaderCallbacksProxy?.DownloadCompletedAdvertisement(resource, data);
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case
                
                _fileDownloaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void StateChangedAdvertisement(string resource, EAndroidFileDownloaderState oldState, EAndroidFileDownloaderState newState) 
            {
                base.StateChangedAdvertisement(resource, oldState, newState); //just in case

                StateChangedAdvertisement(
                    resource: resource, //essentially the remote filepath
                    oldState: TranslateEAndroidFileDownloaderState(oldState),
                    newState: TranslateEAndroidFileDownloaderState(newState)
                );
            }

            public void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState)
            {
                _fileDownloaderCallbacksProxy?.StateChangedAdvertisement(
                    resource: resource,
                    oldState: oldState,
                    newState: newState);
            }

            public override void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
            {
                base.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput); //just in case

                _fileDownloaderCallbacksProxy?.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
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