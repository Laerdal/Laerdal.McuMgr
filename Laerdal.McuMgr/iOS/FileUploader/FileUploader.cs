// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Enums;
using Laerdal.McuMgr.FileUploader.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        public FileUploader(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }
        
        static private INativeFileUploaderProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeFileUploaderProxy(
                bluetoothDevice: bluetoothDevice,
                nativeFileUploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy()
            );
        }

        //ReSharper disable once InconsistentNaming
        private sealed class IOSNativeFileUploaderProxy : IOSListenerForFileUploader, INativeFileUploaderProxy
        {
            private IOSFileUploader _nativeFileUploader;
            private readonly INativeFileUploaderCallbacksProxy _nativeFileUploaderCallbacksProxy;
            
            internal IOSNativeFileUploaderProxy(CBPeripheral bluetoothDevice, INativeFileUploaderCallbacksProxy nativeFileUploaderCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFileUploaderCallbacksProxy = nativeFileUploaderCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFileUploaderCallbacksProxy));

                _nativeFileUploader = new IOSFileUploader(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFileUploaderCallbacksProxy = nativeFileUploaderCallbacksProxy; //composition-over-inheritance
            }
            

            #region commands
            
            public string LastFatalErrorMessage => _nativeFileUploader?.LastFatalErrorMessage;
            
            public void Cancel() => _nativeFileUploader?.Cancel();
            public void Disconnect() => _nativeFileUploader?.Disconnect();

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
                    CleanupResourcesOfLastUpload(); // shouldnt be necessary   but just in case
                }

                _alreadyDisposed = true;
                
                base.Dispose(disposing);
            }
            
            private void CleanupInfrastructure()
            {
                _nativeFileUploader?.Dispose();
                _nativeFileUploader = null;
            }

            public void CleanupResourcesOfLastUpload() //00
            {
                _nsDataOfFileInCurrentlyActiveUpload?.Dispose();
                _nsDataOfFileInCurrentlyActiveUpload = null;
                
                //00 the method needs to be public so that it can be called manually when someone uses BeginUpload() instead of UploadAsync()!
            }

            private NSData _nsDataOfFileInCurrentlyActiveUpload;
            public EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var nsDataOfFileToUpload = NSData.FromArray(data);

                var verdict = TranslateFileUploaderVerdict(_nativeFileUploader.BeginUpload(
                    data: nsDataOfFileToUpload,
                    remoteFilePath: remoteFilePath
                ));
                if (verdict != EFileUploaderVerdict.Success)
                {
                    nsDataOfFileToUpload.Dispose();
                    return verdict;
                }
                
                _nsDataOfFileInCurrentlyActiveUpload = nsDataOfFileToUpload;
                return EFileUploaderVerdict.Success;
            }
            
            public bool TrySetContext(object context)
            {
                return true; //nothing to do in ios   only android needs this and supports it
            }

            public bool TrySetBluetoothDevice(object bluetoothDevice)
            {
                var iosBluetoothDevice = bluetoothDevice as CBPeripheral ?? throw new ArgumentException($"Expected {nameof(bluetoothDevice)} to be of type {nameof(CBPeripheral)}", nameof(bluetoothDevice));
                
                return _nativeFileUploader?.TrySetBluetoothDevice(iosBluetoothDevice) ?? false;
            }

            public bool TryInvalidateCachedTransport()
            {               
                return _nativeFileUploader?.TryInvalidateCachedTransport() ?? false;
            }

            #endregion commands


            
            #region ios listener callbacks -> csharp event emitters

            public IFileUploaderEventEmittable FileUploader
            {
                get => _nativeFileUploaderCallbacksProxy!.FileUploader;
                set => _nativeFileUploaderCallbacksProxy!.FileUploader = value;
            }
            
            public override void CancelledAdvertisement()
                => _nativeFileUploaderCallbacksProxy?.CancelledAdvertisement();

            public override void LogMessageAdvertisement(string message, string category, string level, string resource)
                => LogMessageAdvertisement(
                    level: HelpersIOS.TranslateEIOSLogLevel(level),
                    message: message,
                    category: category,
                    resource: resource
                );

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource) //conformance to the interface
                => _nativeFileUploaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                );

            public override void StateChangedAdvertisement(string resource, EIOSFileUploaderState oldState, EIOSFileUploaderState newState)
                => StateChangedAdvertisement(
                    resource: resource, //essentially the remote filepath
                    newState: TranslateEIOSFileUploaderState(newState),
                    oldState: TranslateEIOSFileUploaderState(oldState)
                );
            public void StateChangedAdvertisement(string resource, EFileUploaderState oldState, EFileUploaderState newState) //conformance to the interface
                => _nativeFileUploaderCallbacksProxy?.StateChangedAdvertisement(
                    resource: resource, //essentially the remote filepath
                    newState: newState,
                    oldState: oldState
                );

            public override void FileUploadedAdvertisement(string resource)
                => _nativeFileUploaderCallbacksProxy?.FileUploadedAdvertisement(resource);

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _nativeFileUploaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);

            public override void FatalErrorOccurredAdvertisement(
                string resource,
                string errorMessage,
                nint mcuMgrErrorCode
            ) => FatalErrorOccurredAdvertisement(
                resource,
                errorMessage,
                (EMcuMgrErrorCode)(int)mcuMgrErrorCode,
                EFileUploaderGroupReturnCode.Unset
            );
            public void FatalErrorOccurredAdvertisement( //conformance to the interface
                string resource,
                string errorMessage, // ReSharper disable once MethodOverloadWithOptionalParameter
                EMcuMgrErrorCode mcuMgrErrorCode,
                EFileUploaderGroupReturnCode fileUploaderGroupReturnCode
            ) => _nativeFileUploaderCallbacksProxy?.FatalErrorOccurredAdvertisement(
                resource,
                errorMessage,
                mcuMgrErrorCode,
                fileUploaderGroupReturnCode
            );

            public override void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(nint progressPercentage, float averageThroughput)
                => FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: (int)progressPercentage
                );
            public void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput) //conformance to the interface
                => _nativeFileUploaderCallbacksProxy?.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                );
            
            #endregion

            static private EFileUploaderVerdict TranslateFileUploaderVerdict(EIOSFileUploadingInitializationVerdict verdict)
            {
                if (verdict == EIOSFileUploadingInitializationVerdict.Success) //0
                {
                    return EFileUploaderVerdict.Success;
                }
            
                if (verdict == EIOSFileUploadingInitializationVerdict.FailedInvalidSettings)
                {
                    return EFileUploaderVerdict.FailedInvalidSettings;
                }

                if (verdict == EIOSFileUploadingInitializationVerdict.FailedInvalidData)
                {
                    return EFileUploaderVerdict.FailedInvalidData;
                }
            
                if (verdict == EIOSFileUploadingInitializationVerdict.FailedOtherUploadAlreadyInProgress)
                {
                    return EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress;
                }

                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value");

                //0 we have to separate enums
                //
                //  - EFileUploaderVerdict which is publicly exposed and used by both IOS and ios
                //  - EIOSFileUploaderVerdict which is specific to IOS and should not be used by the api surface or the end users
            }

            // ReSharper disable once InconsistentNaming
            static private EFileUploaderState TranslateEIOSFileUploaderState(EIOSFileUploaderState state) => state switch
            {
                EIOSFileUploaderState.None => EFileUploaderState.None,
                EIOSFileUploaderState.Idle => EFileUploaderState.Idle,
                EIOSFileUploaderState.Error => EFileUploaderState.Error,
                EIOSFileUploaderState.Paused => EFileUploaderState.Paused,
                EIOSFileUploaderState.Complete => EFileUploaderState.Complete,
                EIOSFileUploaderState.Uploading => EFileUploaderState.Uploading,
                EIOSFileUploaderState.Cancelled => EFileUploaderState.Cancelled,
                EIOSFileUploaderState.Cancelling => EFileUploaderState.Cancelling,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value")
            };
        }
    }
}