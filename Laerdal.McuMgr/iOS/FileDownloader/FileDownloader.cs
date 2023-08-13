// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Contracts;
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
            private readonly IOSFileDownloader _nativeIosFileDownloader;
            private readonly INativeFileDownloaderCallbacksProxy _nativeFileDownloaderCallbacksProxy;

            public IFileDownloaderEventEmitters FileDownloader { get; set; }
            
            internal IOSNativeFileDownloaderProxy(CBPeripheral bluetoothDevice, INativeFileDownloaderCallbacksProxy nativeFileDownloaderCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFileDownloaderCallbacksProxy = nativeFileDownloaderCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderCallbacksProxy));

                _nativeIosFileDownloader = new IOSFileDownloader(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFileDownloaderCallbacksProxy = nativeFileDownloaderCallbacksProxy; //composition-over-inheritance
            }
            

            #region commands

            public string RemoteFilePath { get; set; }
            
            public string LastFatalErrorMessage => _nativeIosFileDownloader?.LastFatalErrorMessage;
            
            public void Cancel() => _nativeIosFileDownloader?.Cancel();
            
            public void Disconnect() => _nativeIosFileDownloader?.Disconnect();
            
            public EFileDownloaderVerdict BeginDownload(string remoteFilePath)
                => TranslateFileDownloaderVerdict(_nativeIosFileDownloader.BeginDownload(remoteFilePath));

            #endregion commands
            
            
            
            
            #region listener callbacks -> event emitters
            
            public override void CancelledAdvertisement()
                => _nativeFileDownloaderCallbacksProxy?.CancelledAdvertisement();

            public override void LogMessageAdvertisement(string message, string category, string level)
                => LogMessageAdvertisement(
                    message,
                    category,
                    HelpersIOS.TranslateEIOSLogLevel(level),
                    RemoteFilePath //todo   this should be emitted by the ios native code really
                );
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                );

            public override void StateChangedAdvertisement(EIOSFileDownloaderState oldState, EIOSFileDownloaderState newState)
                => StateChangedAdvertisement(
                    newState: TranslateEIOSFileDownloaderState(newState),
                    oldState: TranslateEIOSFileDownloaderState(oldState)
                );
            public void StateChangedAdvertisement(EFileDownloaderState oldState, EFileDownloaderState newState) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.StateChangedAdvertisement(
                    newState: newState,
                    oldState: oldState
                );

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _nativeFileDownloaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            
            public override void DownloadCompletedAdvertisement(NSNumber[] data)
            {
                var dataBytes = new byte[data.Length];
                for (var i = 0; i < data.Length; i++)
                {
                    dataBytes[i] = data[i].ByteValue;
                }

                DownloadCompletedAdvertisement(dataBytes);
            }

            public void DownloadCompletedAdvertisement(byte[] data) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.DownloadCompletedAdvertisement(data);

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
                => _nativeFileDownloaderCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage);

            public override void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(nint progressPercentage, float averageThroughput)
                => FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(
                    averageThroughput: averageThroughput,
                    progressPercentage: (int)progressPercentage
                );
            public void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(
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

                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null);

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
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}