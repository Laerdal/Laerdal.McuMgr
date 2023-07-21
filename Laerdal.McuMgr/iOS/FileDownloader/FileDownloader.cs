// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Events;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
        private readonly IOSFileDownloader _iosFileDownloaderProxy;

        public FileDownloader(CBPeripheral bleDevice)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));

            _iosFileDownloaderProxy = new IOSFileDownloader(
                listener: new IOSFileDownloaderListenerProxy(this),
                cbPeripheral: bleDevice
            );
        }

        public string LastFatalErrorMessage => _iosFileDownloaderProxy?.LastFatalErrorMessage;

        public IFileDownloader.EFileDownloaderVerdict BeginDownload(string remoteFilePath)
        {
            if (string.IsNullOrWhiteSpace(remoteFilePath))
                throw new InvalidOperationException($"The {nameof(remoteFilePath)} parameter is dud!");

            remoteFilePath = remoteFilePath.Trim();
            if (remoteFilePath.EndsWith("/")) //00
                throw new InvalidOperationException($"The given {nameof(remoteFilePath)} points to a directory not a file!");

            if (!remoteFilePath.StartsWith("/")) //10
            {
                remoteFilePath = $"/{remoteFilePath}";
            }

            var verdict = _iosFileDownloaderProxy.BeginDownload(remoteFilePath: remoteFilePath);

            return TranslateFileDownloaderVerdict(verdict);

            //00  we spot this very common mistake and stop it right here    otherwise it causes a very cryptic error
            //10  the target file path must be absolute   if its not then we make it so   relative paths cause exotic errors
        }

        public void Cancel() => _iosFileDownloaderProxy?.Cancel();
        public void Disconnect() => _iosFileDownloaderProxy?.Disconnect();

        static private IFileDownloader.EFileDownloaderVerdict TranslateFileDownloaderVerdict(EIOSFileDownloadingInitializationVerdict verdict)
        {
            if (verdict == EIOSFileDownloadingInitializationVerdict.Success) //0
            {
                return IFileDownloader.EFileDownloaderVerdict.Success;
            }

            if (verdict == EIOSFileDownloadingInitializationVerdict.FailedInvalidSettings)
            {
                return IFileDownloader.EFileDownloaderVerdict.FailedInvalidSettings;
            }
            
            if (verdict == EIOSFileDownloadingInitializationVerdict.FailedDownloadAlreadyInProgress)
            {
                return IFileDownloader.EFileDownloaderVerdict.FailedDownloadAlreadyInProgress;
            }

            throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null);

            //0 we have to separate enums
            //
            //  - EFileDownloaderVerdict which is publicly exposed and used by both IOS and ios
            //  - EIOSFileDownloaderVerdict which is specific to IOS and should not be used by the api surface or the end users
        }

        //ReSharper disable once InconsistentNaming
        private sealed class IOSFileDownloaderListenerProxy : IOSListenerForFileDownloader
        {
            private readonly FileDownloader _fileDownloader;

            internal IOSFileDownloaderListenerProxy(FileDownloader fileDownloader)
            {
                _fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
            }

            public override void CancelledAdvertisement() => _fileDownloader?.OnCancelled(new CancelledEventArgs());
            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _fileDownloader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));
            public override void FatalErrorOccurredAdvertisement(string errorMessage) => _fileDownloader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));

            public override void LogMessageAdvertisement(string message, string category, string level)
                => _fileDownloader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: HelpersIOS.TranslateEIOSLogLevel(level),
                    message: message,
                    category: category,
                    resource: "file-downloader"
                ));

            public override void StateChangedAdvertisement(EIOSFileDownloaderState oldState, EIOSFileDownloaderState newState)
                => _fileDownloader?.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEIOSFileDownloaderState(newState),
                    oldState: TranslateEIOSFileDownloaderState(oldState)
                ));

            public override void DownloadCompletedAdvertisement(NSNumber[] data)
            {
                var dataBytes = new byte[data.Length];
                for (var i = 0; i < data.Length; i++)
                {
                    dataBytes[i] = data[i].ByteValue;
                }
                
                _fileDownloader?.OnDownloadCompleted(new DownloadCompletedEventArgs(dataBytes));
            }

            public override void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(nint progressPercentage, float averageThroughput)
                => _fileDownloader?.OnFileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(new FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: (int)progressPercentage
                ));

            // ReSharper disable once InconsistentNaming
            static private IFileDownloader.EFileDownloaderState TranslateEIOSFileDownloaderState(EIOSFileDownloaderState state) => state switch
            {
                EIOSFileDownloaderState.None => IFileDownloader.EFileDownloaderState.None,
                EIOSFileDownloaderState.Idle => IFileDownloader.EFileDownloaderState.Idle,
                EIOSFileDownloaderState.Error => IFileDownloader.EFileDownloaderState.Error,
                EIOSFileDownloaderState.Paused => IFileDownloader.EFileDownloaderState.Paused,
                EIOSFileDownloaderState.Complete => IFileDownloader.EFileDownloaderState.Complete,
                EIOSFileDownloaderState.Cancelled => IFileDownloader.EFileDownloaderState.Cancelled,
                EIOSFileDownloaderState.Cancelling => IFileDownloader.EFileDownloaderState.Cancelling,
                EIOSFileDownloaderState.Downloading => IFileDownloader.EFileDownloaderState.Downloading,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}