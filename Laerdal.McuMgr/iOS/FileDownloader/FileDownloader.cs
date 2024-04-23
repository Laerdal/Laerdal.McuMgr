// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts;
using Laerdal.McuMgr.FileDownloader.Contracts.Enums;
using Laerdal.McuMgr.FileDownloader.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
        public FileDownloader(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }
        
        static private INativeFileDownloaderProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeFileDownloaderProxy(
                bluetoothDevice: bluetoothDevice,
                nativeFileDownloaderCallbacksProxy: new GenericNativeFileDownloaderCallbacksProxy()
            );
        }

        //ReSharper disable once InconsistentNaming
        private sealed class IOSNativeFileDownloaderProxy : IOSListenerForFileDownloader, INativeFileDownloaderProxy
        {
            private IOSFileDownloader _nativeFileDownloader;
            private readonly INativeFileDownloaderCallbacksProxy _nativeFileDownloaderCallbacksProxy;

            internal IOSNativeFileDownloaderProxy(CBPeripheral bluetoothDevice, INativeFileDownloaderCallbacksProxy nativeFileDownloaderCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFileDownloaderCallbacksProxy = nativeFileDownloaderCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderCallbacksProxy));

                _nativeFileDownloader = new IOSFileDownloader(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFileDownloaderCallbacksProxy = nativeFileDownloaderCallbacksProxy; //composition-over-inheritance
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

                _nativeFileDownloader?.Dispose();
                _nativeFileDownloader = null;
            }

            #region commands
            
            public string LastFatalErrorMessage => _nativeFileDownloader?.LastFatalErrorMessage;
            
            public void Cancel() => _nativeFileDownloader?.Cancel();
            
            public void Disconnect() => _nativeFileDownloader?.Disconnect();
            
            public EFileDownloaderVerdict BeginDownload(string remoteFilePath)
                => TranslateFileDownloaderVerdict(_nativeFileDownloader.BeginDownload(remoteFilePath));

            #endregion commands


            #region ios listener callbacks -> csharp event emitters

            public IFileDownloaderEventEmittable FileDownloader
            {
                get => _nativeFileDownloaderCallbacksProxy!.FileDownloader;
                set => _nativeFileDownloaderCallbacksProxy!.FileDownloader = value;
            }

            public override void CancelledAdvertisement()
                => _nativeFileDownloaderCallbacksProxy?.CancelledAdvertisement();

            public override void LogMessageAdvertisement(string message, string category, string level, string resource)
                => LogMessageAdvertisement(
                    message,
                    category,
                    HelpersIOS.TranslateEIOSLogLevel(level),
                    resource
                );
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                );

            public override void StateChangedAdvertisement(string resource, EIOSFileDownloaderState oldState, EIOSFileDownloaderState newState)
                => StateChangedAdvertisement(
                    resource: resource, //essentially the remote filepath
                    newState: TranslateEIOSFileDownloaderState(newState),
                    oldState: TranslateEIOSFileDownloaderState(oldState)
                );
            public void StateChangedAdvertisement(string resource, EFileDownloaderState oldState, EFileDownloaderState newState) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.StateChangedAdvertisement(
                    resource: resource, //essentially the remote filepath
                    newState: newState,
                    oldState: oldState
                );

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _nativeFileDownloaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            
            public override void DownloadCompletedAdvertisement(string resource, NSNumber[] data)
            {
                var dataBytes = new byte[data.Length];
                for (var i = 0; i < data.Length; i++)
                {
                    dataBytes[i] = data[i].ByteValue;
                }

                DownloadCompletedAdvertisement(resource, dataBytes);
            }

            public void DownloadCompletedAdvertisement(string resource, byte[] data) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.DownloadCompletedAdvertisement(resource, data);

            public override void FatalErrorOccurredAdvertisement(string resource, string errorMessage)
                => _nativeFileDownloaderCallbacksProxy?.FatalErrorOccurredAdvertisement(resource, errorMessage);

            public override void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(nint progressPercentage, float averageThroughput)
                => FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: (int)progressPercentage
                );
            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                );
            
            #endregion

                        
            static private EFileDownloaderVerdict TranslateFileDownloaderVerdict(EIOSFileDownloadingInitializationVerdict verdict)
            {
                if (verdict == EIOSFileDownloadingInitializationVerdict.Success) //0
                {
                    return EFileDownloaderVerdict.Success;
                }

                if (verdict == EIOSFileDownloadingInitializationVerdict.FailedInvalidSettings)
                {
                    return EFileDownloaderVerdict.FailedInvalidSettings;
                }
            
                if (verdict == EIOSFileDownloadingInitializationVerdict.FailedDownloadAlreadyInProgress)
                {
                    return EFileDownloaderVerdict.FailedDownloadAlreadyInProgress;
                }

                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value");

                //0 we have to separate enums
                //
                //  - EFileDownloaderVerdict which is publicly exposed and used by both IOS and ios
                //  - EIOSFileDownloaderVerdict which is specific to IOS and should not be used by the api surface or the end users
            }

            // ReSharper disable once InconsistentNaming
            static private EFileDownloaderState TranslateEIOSFileDownloaderState(EIOSFileDownloaderState state) => state switch
            {
                EIOSFileDownloaderState.None => EFileDownloaderState.None,
                EIOSFileDownloaderState.Idle => EFileDownloaderState.Idle,
                EIOSFileDownloaderState.Error => EFileDownloaderState.Error,
                EIOSFileDownloaderState.Paused => EFileDownloaderState.Paused,
                EIOSFileDownloaderState.Complete => EFileDownloaderState.Complete,
                EIOSFileDownloaderState.Cancelled => EFileDownloaderState.Cancelled,
                EIOSFileDownloaderState.Cancelling => EFileDownloaderState.Cancelling,
                EIOSFileDownloaderState.Downloading => EFileDownloaderState.Downloading,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value")
            };
        }
    }
}