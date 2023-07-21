// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Events;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        private readonly IOSFileUploader _iosFileUploaderProxy;
        
        public FileUploader(CBPeripheral bleDevice)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));
            
            _iosFileUploaderProxy = new IOSFileUploader(
                listener: new IOSFileUploaderListenerProxy(this),
                cbPeripheral: bleDevice
            );
        }

        public string LastFatalErrorMessage => _iosFileUploaderProxy?.LastFatalErrorMessage;

        public IFileUploader.EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
        {
            // if (data == null || !data.Any())
            //     throw new InvalidOperationException($"The '{nameof(data)}' byte-array parameter is null or empty");
            
            if (string.IsNullOrWhiteSpace(remoteFilePath))
                throw new InvalidOperationException($"The {nameof(remoteFilePath)} parameter is dud!");

            remoteFilePath = remoteFilePath.Trim();
            if (remoteFilePath.EndsWith("/")) //00
                throw new InvalidOperationException($"The given {nameof(remoteFilePath)} points to a directory not a file!");
            
            if (!remoteFilePath.StartsWith("/")) //10
            {
                remoteFilePath = $"/{remoteFilePath}";
            }
            
            var nsData = NSData.FromArray(data);

            var verdict = _iosFileUploaderProxy.BeginUpload(
                data: nsData,
                remoteFilePath: remoteFilePath
            );

            return TranslateFileUploaderVerdict(verdict);
            
            //00  we spot this very common mistake and stop it right here    otherwise it causes a very cryptic error
            //10  the target file path must be absolute   if its not then we make it so   relative paths cause exotic errors
        }

        public void Cancel() => _iosFileUploaderProxy?.Cancel();
        public void Disconnect() => _iosFileUploaderProxy?.Disconnect();
        
        static private IFileUploader.EFileUploaderVerdict TranslateFileUploaderVerdict(EIOSFileUploadingInitializationVerdict verdict)
        {
            if (verdict == EIOSFileUploadingInitializationVerdict.Success) //0
            {
                return IFileUploader.EFileUploaderVerdict.Success;
            }
            
            if (verdict == EIOSFileUploadingInitializationVerdict.FailedInvalidSettings)
            {
                return IFileUploader.EFileUploaderVerdict.FailedInvalidSettings;
            }

            if (verdict == EIOSFileUploadingInitializationVerdict.FailedInvalidData)
            {
                return IFileUploader.EFileUploaderVerdict.FailedInvalidData;
            }
            
            if (verdict == EIOSFileUploadingInitializationVerdict.FailedOtherUploadAlreadyInProgress)
            {
                return IFileUploader.EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress;
            }

            throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null);

            //0 we have to separate enums
            //
            //  - EFileUploaderVerdict which is publicly exposed and used by both IOS and ios
            //  - EIOSFileUploaderVerdict which is specific to IOS and should not be used by the api surface or the end users
        }
        
        
        // ReSharper disable once InconsistentNaming
        private sealed class IOSFileUploaderListenerProxy : IOSListenerForFileUploader
        {
            private readonly FileUploader _fileUploader;

            internal IOSFileUploaderListenerProxy(FileUploader fileUploader)
            {
                _fileUploader = fileUploader ?? throw new ArgumentNullException(nameof(fileUploader));
            }

            public override void CancelledAdvertisement(string remoteFilePath) => _fileUploader.OnCancelled(new CancelledEventArgs(remoteFilePath));
            public override void BusyStateChangedAdvertisement(string remoteFilePath, bool busyNotIdle) => _fileUploader.OnBusyStateChanged(new BusyStateChangedEventArgs(remoteFilePath, busyNotIdle));
            public override void FatalErrorOccurredAdvertisement(string remoteFilePath, string errorMessage) => _fileUploader.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(remoteFilePath, errorMessage));

            public override void LogMessageAdvertisement(string remoteFilePath, string message, string category, string level)
                => _fileUploader.OnLogEmitted(new LogEmittedEventArgs(
                    level: HelpersIOS.TranslateEIOSLogLevel(level),
                    message: message,
                    category: category,
                    resource: remoteFilePath
                ));

            public override void StateChangedAdvertisement(string remoteFilePath, EIOSFileUploaderState oldState, EIOSFileUploaderState newState)
                => _fileUploader.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEIOSFileUploaderState(newState),
                    oldState: TranslateEIOSFileUploaderState(oldState),
                    remoteFilePath: remoteFilePath
                ));

            public override void FileUploadProgressPercentageAndThroughputDataChangedAdvertisement(string remoteFilePath, nint progressPercentage, float averageThroughput)
                => _fileUploader.OnFileUploadProgressPercentageAndThroughputDataChangedAdvertisement(new FileUploadProgressPercentageAndDataThroughputChangedEventArgs(
                    remoteFilePath: remoteFilePath,
                    averageThroughput: averageThroughput,
                    progressPercentage: (int)progressPercentage
                ));

            // ReSharper disable once InconsistentNaming
            static private IFileUploader.EFileUploaderState TranslateEIOSFileUploaderState(EIOSFileUploaderState state) => state switch
            {
                EIOSFileUploaderState.None => IFileUploader.EFileUploaderState.None,
                EIOSFileUploaderState.Idle => IFileUploader.EFileUploaderState.Idle,
                EIOSFileUploaderState.Error => IFileUploader.EFileUploaderState.Error,
                EIOSFileUploaderState.Paused => IFileUploader.EFileUploaderState.Paused,
                EIOSFileUploaderState.Complete => IFileUploader.EFileUploaderState.Complete,
                EIOSFileUploaderState.Uploading => IFileUploader.EFileUploaderState.Uploading,
                EIOSFileUploaderState.Cancelled => IFileUploader.EFileUploaderState.Cancelled,
                EIOSFileUploaderState.Cancelling => IFileUploader.EFileUploaderState.Cancelling,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}
