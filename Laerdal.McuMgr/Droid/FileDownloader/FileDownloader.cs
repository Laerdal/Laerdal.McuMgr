// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;

using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileDownloader.Events;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
        private readonly AndroidFileDownloaderProxy _androidFileDownloaderProxy;

        public FileDownloader(BluetoothDevice bleDevice, Context androidContext = null)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            _androidFileDownloaderProxy = new AndroidFileDownloaderProxy(
                downloader: this,
                context: androidContext,
                bluetoothDevice: bleDevice
            );
        }

        public string LastFatalErrorMessage => _androidFileDownloaderProxy?.LastFatalErrorMessage;

        public IFileDownloader.EFileDownloaderVerdict BeginDownload(string remoteFilePath)
        {           
            if (string.IsNullOrWhiteSpace(remoteFilePath))
                throw new ArgumentException($"The {nameof(remoteFilePath)} parameter is dud!");

            remoteFilePath = remoteFilePath.Trim();
            if (remoteFilePath.EndsWith("/")) //00
                throw new ArgumentException($"The given {nameof(remoteFilePath)} points to a directory not a file!");

            if (!remoteFilePath.StartsWith("/")) //10
            {
                remoteFilePath = $"/{remoteFilePath}";
            }

            _androidFileDownloaderProxy.RemoteFilePath = remoteFilePath;
            var verdict = _androidFileDownloaderProxy.BeginDownload(remoteFilePath: remoteFilePath);

            return TranslateFileDownloaderVerdict(verdict);

            //00  we spot this very common mistake and stop it right here    otherwise it causes a very cryptic error
            //10  the target file path must be absolute   if its not then we make it so   relative paths cause exotic errors
        }

        public void Cancel() => _androidFileDownloaderProxy?.Cancel();
        public void Disconnect() => _androidFileDownloaderProxy?.Disconnect();

        static private IFileDownloader.EFileDownloaderVerdict TranslateFileDownloaderVerdict(EAndroidFileDownloaderVerdict verdict)
        {
            if (verdict == EAndroidFileDownloaderVerdict.Success) //0
            {
                return IFileDownloader.EFileDownloaderVerdict.Success;
            }
            
            if (verdict == EAndroidFileDownloaderVerdict.FailedInvalidSettings)
            {
                return IFileDownloader.EFileDownloaderVerdict.FailedInvalidSettings;
            }
            
            if (verdict == EAndroidFileDownloaderVerdict.FailedDownloadAlreadyInProgress)
            {
                return IFileDownloader.EFileDownloaderVerdict.FailedDownloadAlreadyInProgress;
            }

            throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null);

            //0 we have to separate enums
            //
            //  - EFileDownloaderVerdict which is publicly exposed and used by both android and ios
            //  - EAndroidFileDownloaderVerdict which is specific to android and should not be used by the api surface or the end users
        }

        private sealed class AndroidFileDownloaderProxy : AndroidFileDownloader
        {
            public string RemoteFilePath { get; set; }
            
            private readonly FileDownloader _fileDownloader;

            // ReSharper disable once UnusedMember.Local
            private AndroidFileDownloaderProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidFileDownloaderProxy(FileDownloader downloader, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _fileDownloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage);

                _fileDownloader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
            }
            
            public override void LogMessageAdvertisement(string message, string category, string level)
            {
                base.LogMessageAdvertisement(message, category, level);

                _fileDownloader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category,
                    resource: RemoteFilePath
                ));
            }

            public override void CancelledAdvertisement()
            {
                base.CancelledAdvertisement(); //just in case
                
                _fileDownloader?.OnCancelled(new CancelledEventArgs());
            }
            
            public override void DownloadCompletedAdvertisement(byte[] data)
            {
                base.DownloadCompletedAdvertisement(data); //just in case
                
                _fileDownloader?.OnDownloadCompleted(new DownloadCompletedEventArgs(data));
            }

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case
                
                _fileDownloader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));
            }

            public override void StateChangedAdvertisement(EAndroidFileDownloaderState oldState, EAndroidFileDownloaderState newState) 
            {
                base.StateChangedAdvertisement(oldState, newState); //just in case

                _fileDownloader?.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEAndroidFileDownloaderState(newState),
                    oldState: TranslateEAndroidFileDownloaderState(oldState)
                ));
            }

            public override void FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(int progressPercentage, float averageThroughput)
            {
                base.FileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(progressPercentage, averageThroughput); //just in case

                _fileDownloader?.OnFileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(new FileDownloadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
            }

            static private IFileDownloader.EFileDownloaderState TranslateEAndroidFileDownloaderState(EAndroidFileDownloaderState state)
            {
                if (state == EAndroidFileDownloaderState.None)
                {
                    return IFileDownloader.EFileDownloaderState.None;
                }
                
                if (state == EAndroidFileDownloaderState.Idle)
                {
                    return IFileDownloader.EFileDownloaderState.Idle;
                }

                if (state == EAndroidFileDownloaderState.Downloading)
                {
                    return IFileDownloader.EFileDownloaderState.Downloading;
                }

                if (state == EAndroidFileDownloaderState.Paused)
                {
                    return IFileDownloader.EFileDownloaderState.Paused;
                }

                if (state == EAndroidFileDownloaderState.Complete)
                {
                    return IFileDownloader.EFileDownloaderState.Complete;
                }
                
                if (state == EAndroidFileDownloaderState.Cancelled)
                {
                    return IFileDownloader.EFileDownloaderState.Cancelled;
                }
                
                if (state == EAndroidFileDownloaderState.Error)
                {
                    return IFileDownloader.EFileDownloaderState.Error;
                }

                if (state == EAndroidFileDownloaderState.Cancelling)
                {
                    return IFileDownloader.EFileDownloaderState.Cancelling;
                }

                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}