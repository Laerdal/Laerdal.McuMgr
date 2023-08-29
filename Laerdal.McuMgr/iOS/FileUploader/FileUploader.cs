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
            private readonly IOSFileUploader _nativeIosFileUploader;
            private readonly INativeFileUploaderCallbacksProxy _nativeFileUploaderCallbacksProxy;

            public IFileUploaderEventEmittable FileUploader { get; set; }
            
            internal IOSNativeFileUploaderProxy(CBPeripheral bluetoothDevice, INativeFileUploaderCallbacksProxy nativeFileUploaderCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFileUploaderCallbacksProxy = nativeFileUploaderCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFileUploaderCallbacksProxy));

                _nativeIosFileUploader = new IOSFileUploader(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFileUploaderCallbacksProxy = nativeFileUploaderCallbacksProxy; //composition-over-inheritance
            }
            

            #region commands
            
            public string LastFatalErrorMessage => _nativeIosFileUploader?.LastFatalErrorMessage;
            
            public void Cancel() => _nativeIosFileUploader?.Cancel();
            
            public void Disconnect() => _nativeIosFileUploader?.Disconnect();
            
            public EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
            {
                var nsData = NSData.FromArray(data); //todo   should nsdata be tracked in a private variable and then cleaned up properly?
                var verdict = TranslateFileUploaderVerdict(_nativeIosFileUploader.BeginUpload(remoteFilePath, nsData));

                return verdict;
            }

            #endregion commands


            
            #region ios listener callbacks -> csharp event emitters
            
            public override void CancelledAdvertisement()
                => _nativeFileUploaderCallbacksProxy?.CancelledAdvertisement();

            public override void LogMessageAdvertisement(string message, string category, string level, string resource)
                => LogMessageAdvertisement(
                    message,
                    category,
                    HelpersIOS.TranslateEIOSLogLevel(level),
                    resource
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

            public override void UploadCompletedAdvertisement(string resource)
                => _nativeFileUploaderCallbacksProxy?.UploadCompletedAdvertisement(resource);
            
            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _nativeFileUploaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);

            public override void FatalErrorOccurredAdvertisement(string resource, string errorMessage)
                => _nativeFileUploaderCallbacksProxy?.FatalErrorOccurredAdvertisement(resource, errorMessage);

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