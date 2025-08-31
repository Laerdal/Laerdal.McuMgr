// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Linq;
using CoreBluetooth;
using Foundation;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FileDownloading
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
        public FileDownloader(object nativeBluetoothDevice) // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            : this(NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<CBPeripheral>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice)))
        {
        }

        public FileDownloader(CBPeripheral nativeBluetoothDevice) : this(ValidateArgumentsAndConstructProxy(nativeBluetoothDevice))
        {
        }
        
        static private INativeFileDownloaderProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeFileDownloaderProxy(
                bluetoothDevice: bluetoothDevice,
                nativeFileDownloaderCallbacksProxy: new FileDownloader.GenericNativeFileDownloaderCallbacksProxy()
            );
        }

        //ReSharper disable once InconsistentNaming
        private sealed class IOSNativeFileDownloaderProxy : IOSListenerForFileDownloader, INativeFileDownloaderProxy
        {
            private readonly IOSFileDownloader _nativeFileDownloader;
            private readonly INativeFileDownloaderCallbacksProxy _nativeFileDownloaderCallbacksProxy;

            internal IOSNativeFileDownloaderProxy(CBPeripheral bluetoothDevice, INativeFileDownloaderCallbacksProxy nativeFileDownloaderCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFileDownloaderCallbacksProxy = nativeFileDownloaderCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderCallbacksProxy));

                _nativeFileDownloader = new IOSFileDownloader(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFileDownloaderCallbacksProxy = nativeFileDownloaderCallbacksProxy; //composition-over-inheritance
            }

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
                try { _nativeFileDownloader?.NativeDispose(); } catch { /*ignored*/ } //order
                try { _nativeFileDownloader?.Dispose();       } catch { /*ignored*/ } //order
                
                //_nativeFileDownloader = null;     @formatter:on
            }
            
            public bool TrySetContext(object context)
            {
                return true; //nothing to do in ios   only android needs this and supports it
            }

            public bool TrySetBluetoothDevice(object bluetoothDevice)
            {
                var iosBluetoothDevice = bluetoothDevice as CBPeripheral ?? throw new ArgumentException($"Expected {nameof(bluetoothDevice)} to be of type {nameof(CBPeripheral)}", nameof(bluetoothDevice));

                return _nativeFileDownloader?.TrySetBluetoothDevice(iosBluetoothDevice) ?? false;
            }

            public bool TryInvalidateCachedInfrastructure()
            {
                return _nativeFileDownloader?.TryInvalidateCachedInfrastructure() ?? false;
            }

            #region commands
            
            public string LastFatalErrorMessage => _nativeFileDownloader?.LastFatalErrorMessage;

            // ReSharper disable once RedundantOverriddenMember
            public bool TryPause() => _nativeFileDownloader?.TryPause() ?? false;
            
            // ReSharper disable once RedundantOverriddenMember
            public bool TryResume() => _nativeFileDownloader?.TryResume() ?? false;
            
            // ReSharper disable once RedundantOverriddenMember
            public bool TryCancel(string reason = "") => _nativeFileDownloader?.TryCancel(reason) ?? false;
            public bool TryDisconnect() => _nativeFileDownloader?.TryDisconnect() ?? false;

            public EFileDownloaderVerdict BeginDownload(
                string remoteFilePath,
                int? initialMtuSize = null
            )
            {
                return TranslateFileDownloaderVerdict(_nativeFileDownloader.BeginDownload(
                    remoteFilePath: remoteFilePath,
                    initialMtuSize: initialMtuSize ?? -1
                ));
            }

            #endregion commands


            #region ios listener callbacks -> csharp event emitters

            public IFileDownloaderEventEmittable FileDownloader
            {
                get => _nativeFileDownloaderCallbacksProxy!.FileDownloader;
                set => _nativeFileDownloaderCallbacksProxy!.FileDownloader = value;
            }

            public override void CancelledAdvertisement(string reason)
                => _nativeFileDownloaderCallbacksProxy?.CancelledAdvertisement(reason: reason);

            public override void CancellingAdvertisement(string reason)
                => _nativeFileDownloaderCallbacksProxy?.CancellingAdvertisement(reason: reason);

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

            public override void StateChangedAdvertisement(
                string remoteFilePath,
                EIOSFileDownloaderState oldState,
                EIOSFileDownloaderState newState,
                nint totalBytesToBeDownloaded,
                NSNumber[] completeDownloadedData //null unless we reach the 'completed' state
            ) => StateChangedAdvertisement(
                oldState: TranslateEIOSFileDownloaderState(oldState),
                newState: TranslateEIOSFileDownloaderState(newState),
                remoteFilePath: remoteFilePath,
                completeDownloadedData: completeDownloadedData?.Select(x => (byte) x).ToArray() ?? Array.Empty<byte>(),
                totalBytesToBeDownloaded: totalBytesToBeDownloaded
            );

            public void StateChangedAdvertisement(string remoteFilePath, EFileDownloaderState oldState, EFileDownloaderState newState, long totalBytesToBeDownloaded, byte[] completeDownloadedData) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.StateChangedAdvertisement(
                    oldState: oldState,
                    newState: newState,
                    remoteFilePath: remoteFilePath,
                    completeDownloadedData: completeDownloadedData,
                    totalBytesToBeDownloaded: totalBytesToBeDownloaded
                );

            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _nativeFileDownloaderCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);

            public override void FatalErrorOccurredAdvertisement(
                string resourceId,
                string errorMessage,
                nint globalErrorCode
            ) => FatalErrorOccurredAdvertisement(
                resourceId,
                errorMessage,
                (EGlobalErrorCode)(int)globalErrorCode
            );

            public void FatalErrorOccurredAdvertisement(string resourceId, string errorMessage, EGlobalErrorCode globalErrorCode)
                => _nativeFileDownloaderCallbacksProxy?.FatalErrorOccurredAdvertisement(resourceId, errorMessage, globalErrorCode);

            public override void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, nint progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId: resourceId, progressPercentage: (int)progressPercentage, currentThroughputInKBps: currentThroughputInKBps, totalAverageThroughputInKBps: totalAverageThroughputInKBps);
            public void FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(string resourceId, int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps) //conformance to the interface
                => _nativeFileDownloaderCallbacksProxy?.FileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(resourceId: resourceId, progressPercentage: progressPercentage, currentThroughputInKBps: currentThroughputInKBps, totalAverageThroughputInKBps: totalAverageThroughputInKBps);
            
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
                
                if (verdict == EIOSFileDownloadingInitializationVerdict.FailedErrorUponCommencing)
                {
                    return EFileDownloaderVerdict.FailedErrorUponCommencing;
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
                EIOSFileDownloaderState.Resuming => EFileDownloaderState.Resuming,
                EIOSFileDownloaderState.Complete => EFileDownloaderState.Complete,
                EIOSFileDownloaderState.Cancelled => EFileDownloaderState.Cancelled,
                EIOSFileDownloaderState.Cancelling => EFileDownloaderState.Cancelling,
                EIOSFileDownloaderState.Downloading => EFileDownloaderState.Downloading,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value")
            };
        }
    }
}