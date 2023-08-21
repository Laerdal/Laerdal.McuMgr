// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Linq;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FileUploader.Contracts;
using Laerdal.McuMgr.FileUploader.Contracts.Events;

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
                fileUploaderCallbacksProxy: new GenericNativeFileUploaderCallbacksProxy()
            );
        }

        public string LastFatalErrorMessage => _androidFileUploaderProxy?.LastFatalErrorMessage;

        public EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data)
        {
            if (data == null) //its ok if the data is empty   but it cant be null
                throw new InvalidOperationException("The data byte-array parameter is null");
            
            RemoteFilePathHelpers.ValidateRemoteFilePath(remoteFilePath); //                    order
            remoteFilePath = RemoteFilePathHelpers.SanitizeRemoteFilePath(remoteFilePath); //   order

            var verdict = _androidFileUploaderProxy.BeginUpload(remoteFilePath: remoteFilePath, data: data);

            return TranslateFileUploaderVerdict(verdict);
        }

        public void Cancel() => _androidFileUploaderProxy?.Cancel();
        public void Disconnect() => _androidFileUploaderProxy?.Disconnect();

        static private EFileUploaderVerdict TranslateFileUploaderVerdict(EAndroidFileUploaderVerdict verdict)
        {
            if (verdict == EAndroidFileUploaderVerdict.Success) //0
            {
                return EFileUploaderVerdict.Success;
            }
            
            if (verdict == EAndroidFileUploaderVerdict.FailedInvalidSettings)
            {
                return EFileUploaderVerdict.FailedInvalidSettings;
            }

            if (verdict == EAndroidFileUploaderVerdict.FailedInvalidData)
            {
                return EFileUploaderVerdict.FailedInvalidData;
            }
            
            if (verdict == EAndroidFileUploaderVerdict.FailedOtherUploadAlreadyInProgress)
            {
                return EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress;
            }

            throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null);

            //0 we have to separate enums
            //
            //  - EFileUploaderVerdict which is publicly exposed and used by both android and ios
            //  - EAndroidFileUploaderVerdict which is specific to android and should not be used by the api surface or the end users
        }

        private sealed class AndroidFileUploaderProxy : AndroidFileUploader
        {
            private readonly FileUploader _fileUploader;

            // ReSharper disable once UnusedMember.Local
            private AndroidFileUploaderProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidFileUploaderProxy(FileUploader uploader, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _fileUploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
            }

            public override void FatalErrorOccurredAdvertisement(string remoteFilePath, string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(remoteFilePath: remoteFilePath, errorMessage: errorMessage);

                _fileUploader?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(
                    errorMessage: errorMessage,
                    remoteFilePath: remoteFilePath
                ));
            }
            
            public override void LogMessageAdvertisement(string remoteFilePath, string message, string category, string level)
            {
                base.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category,
                    remoteFilePath: remoteFilePath
                );

                _fileUploader?.OnLogEmitted(new LogEmittedEventArgs(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category,
                    resource: remoteFilePath
                ));
            }

            public override void CancelledAdvertisement(string remoteFilePath)
            {
                base.CancelledAdvertisement(remoteFilePath); //just in case
                
                _fileUploader?.OnCancelled(new CancelledEventArgs(remoteFilePath));
            }

            public override void BusyStateChangedAdvertisement(string remoteFilePath, bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(remoteFilePath, busyNotIdle); //just in case
                
                _fileUploader?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));  
            }

            public override void StateChangedAdvertisement(string remoteFilePath, EAndroidFileUploaderState oldState, EAndroidFileUploaderState newState) 
            {
                base.StateChangedAdvertisement(remoteFilePath, oldState, newState); //just in case

                _fileUploader?.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEAndroidFileUploaderState(newState),
                    oldState: TranslateEAndroidFileUploaderState(oldState),
                    resource: remoteFilePath
                ));
            }

            public override void FileUploadProgressPercentageAndThroughputDataChangedAdvertisement(string remoteFilePath, int progressPercentage, float averageThroughput)
            {
                base.FileUploadProgressPercentageAndThroughputDataChangedAdvertisement(remoteFilePath, progressPercentage, averageThroughput); //just in case

                _fileUploader?.OnFileUploadProgressPercentageAndThroughputDataChanged(new FileUploadProgressPercentageAndDataThroughputChangedEventArgs(progressPercentage: progressPercentage, averageThroughput: averageThroughput));
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

                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}