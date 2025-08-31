// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FileUploading
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        public FileUploader(object nativeBluetoothDevice) // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            : this(NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<CBPeripheral>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice)))
        {
        }

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
            private readonly IOSFileUploader _nativeFileUploader;
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
            
            // ReSharper disable once RedundantOverriddenMember
            public bool TryPause() => _nativeFileUploader?.TryPause() ?? false;
            
            // ReSharper disable once RedundantOverriddenMember
            public bool TryResume() => _nativeFileUploader?.TryResume() ?? false;
            
            // ReSharper disable once RedundantOverriddenMember
            public bool TryCancel(string reason = "") => _nativeFileUploader?.TryCancel(reason) ?? false;

            public bool TryDisconnect() => _nativeFileUploader?.TryDisconnect() ?? false;
            
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
                TryCleanupResourcesOfLastUpload();

                _alreadyDisposed = true;

                try
                {
                    base.Dispose(disposing: true);
                }
                catch
                {
                    // ignored
                }
            }

            private void TryCleanupInfrastructure() // @formatter:off
            {
                try { _nativeFileUploader?.NativeDispose(); } catch { /*ignored*/ } //order
                try { _nativeFileUploader?.Dispose();       } catch { /*ignored*/ } //order
                
                //_nativeFileUploader = null;       @formatter:on
            }

            public void TryCleanupResourcesOfLastUpload() //00
            {
                try
                {
                    _nsDataOfFileInCurrentlyActiveUpload?.Dispose();
                    _nsDataOfFileInCurrentlyActiveUpload = null;
                }
                catch
                {
                    //ignored
                }
                
                //00 the method needs to be public so that it can be called manually when someone uses BeginUpload() instead of UploadAsync()!
            }

            private NSData _nsDataOfFileInCurrentlyActiveUpload;
            
            public EFileUploaderVerdict BeginUpload(
                byte[] data,
                string resourceId,
                string remoteFilePath,
                int? initialMtuSize = null,
                int? pipelineDepth = null, //    ios only
                int? byteAlignment = null, //    ios only
                int? windowCapacity = null, //   android only
                int? memoryAlignment = null //   android only
            )
            {
                var nsDataOfFileToUpload = NSData.FromArray(data);

                var verdict = TranslateFileUploaderVerdict(_nativeFileUploader.BeginUpload(
                    data: nsDataOfFileToUpload,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,

                    pipelineDepth: pipelineDepth ?? -1,
                    byteAlignment: byteAlignment ?? -1,
                    initialMtuSize: initialMtuSize ?? -1
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

            public bool TryInvalidateCachedInfrastructure()
            {               
                return _nativeFileUploader?.TryInvalidateCachedInfrastructure() ?? false;
            }

            #endregion commands


            
            #region ios listener callbacks -> csharp event emitters

            public IFileUploaderEventEmittable FileUploader
            {
                get => _nativeFileUploaderCallbacksProxy!.FileUploader;
                set => _nativeFileUploaderCallbacksProxy!.FileUploader = value;
            }
            
            public override void CancellingAdvertisement(string reason)
                => _nativeFileUploaderCallbacksProxy?.CancellingAdvertisement(reason);
            
            public override void CancelledAdvertisement(string reason)
                => _nativeFileUploaderCallbacksProxy?.CancelledAdvertisement(reason);

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
                    resourceId: resource
                );

            public override void StateChangedAdvertisement(string resourceId, string remoteFilePath, EIOSFileUploaderState oldState, EIOSFileUploaderState newState, nint totalBytesToBeUploadedAsNint)
                => StateChangedAdvertisement(
                    newState: TranslateEIOSFileUploaderState(newState),
                    oldState: TranslateEIOSFileUploaderState(oldState),
                    resourceId: resourceId, //essentially the local filepath most of the times
                    remoteFilePath: remoteFilePath, //essentially the remote filepath
                    totalBytesToBeUploaded: totalBytesToBeUploadedAsNint
                );

            public void StateChangedAdvertisement(string resourceId, string remoteFilePath, EFileUploaderState oldState, EFileUploaderState newState, long totalBytesToBeUploaded) //conforms to the interface
                => _nativeFileUploaderCallbacksProxy?.StateChangedAdvertisement(
                    oldState: oldState,
                    newState: newState,
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    totalBytesToBeUploaded: totalBytesToBeUploaded
                );

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _nativeFileUploaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);

            public override void FatalErrorOccurredAdvertisement(
                string resourceId,
                string remoteFilePath,
                string errorMessage,
                nint globalErrorCode
            ) => FatalErrorOccurredAdvertisement(
                resourceId,
                remoteFilePath,
                errorMessage,
                (EGlobalErrorCode)(int)globalErrorCode
            );

            public void FatalErrorOccurredAdvertisement( //conformance to the interface
                string resourceId,
                string remoteFilePath,
                string errorMessage,
                EGlobalErrorCode globalErrorCode
            ) => _nativeFileUploaderCallbacksProxy?.FatalErrorOccurredAdvertisement(
                resourceId,
                remoteFilePath,
                errorMessage,
                globalErrorCode
            );

            public override void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, string remoteFilePath, nint progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    progressPercentage: (int)progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                );
            public void FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, string remoteFilePath, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps) //conformance to the interface
                => _nativeFileUploaderCallbacksProxy?.FileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    progressPercentage: progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                );
            
            #endregion

            static private EFileUploaderVerdict TranslateFileUploaderVerdict(EIOSFileUploadingInitializationVerdict verdict)
            {
                if (verdict == EIOSFileUploadingInitializationVerdict.Success) //0
                {
                    return EFileUploaderVerdict.Success;
                }
                
                if (verdict == EIOSFileUploadingInitializationVerdict.FailedInvalidData)
                {
                    return EFileUploaderVerdict.FailedInvalidData;
                }
            
                if (verdict == EIOSFileUploadingInitializationVerdict.FailedInvalidSettings)
                {
                    return EFileUploaderVerdict.FailedInvalidSettings;
                }

                if (verdict == EIOSFileUploadingInitializationVerdict.FailedErrorUponCommencing)
                {
                    return EFileUploaderVerdict.FailedErrorUponCommencing;
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
                EIOSFileUploaderState.Resuming => EFileUploaderState.Resuming,
                EIOSFileUploaderState.Complete => EFileUploaderState.Complete,
                EIOSFileUploaderState.Uploading => EFileUploaderState.Uploading,
                EIOSFileUploaderState.Cancelled => EFileUploaderState.Cancelled,
                EIOSFileUploaderState.Cancelling => EFileUploaderState.Cancelling,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value")
            };
        }
    }
}