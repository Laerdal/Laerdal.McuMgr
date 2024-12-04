// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Constants;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller, IFirmwareInstallerEventEmittable
    {
        private readonly INativeFirmwareInstallerProxy _nativeFirmwareInstallerProxy;

        public string LastFatalErrorMessage => _nativeFirmwareInstallerProxy?.LastFatalErrorMessage;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFirmwareInstallerProxy
        internal FirmwareInstaller(INativeFirmwareInstallerProxy nativeFirmwareInstallerProxy)
        {
            _nativeFirmwareInstallerProxy = nativeFirmwareInstallerProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareInstallerProxy));
            _nativeFirmwareInstallerProxy.FirmwareInstaller = this; //vital
        }

        public void Dispose()
        {
            _nativeFirmwareInstallerProxy?.Dispose();

            GC.SuppressFinalize(this);
        }
        
        public EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null, //   android only    not applicable for ios
            int? windowCapacity = null, //   android only    not applicable for ios
            int? memoryAlignment = null, //  android only    not applicable for ios
            int? pipelineDepth = null, //    ios only        not applicable for android
            int? byteAlignment = null //     ios only        not applicable for android
        )
        {
            if (data == null || !data.Any())
                throw new ArgumentException("The data byte-array parameter is null or empty", nameof(data));

            var failsafeConnectionSettings = ConnectionSettingsHelpers.GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
                uploadingNotDownloading: true,

                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,

                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                initialMtuSize: initialMtuSize,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );
            if (failsafeConnectionSettings != null)
            {
                pipelineDepth = failsafeConnectionSettings.Value.pipelineDepth;
                byteAlignment = failsafeConnectionSettings.Value.byteAlignment;
                initialMtuSize = failsafeConnectionSettings.Value.initialMtuSize;
                windowCapacity = failsafeConnectionSettings.Value.windowCapacity;
                memoryAlignment = failsafeConnectionSettings.Value.memoryAlignment;
                
                OnLogEmitted(new LogEmittedEventArgs(
                    level: ELogLevel.Warning,
                    message: $"[FI.BI.010] Host device '{hostDeviceModel} (made by {hostDeviceManufacturer})' is known to be problematic. Resorting to using failsafe settings " +
                             $"(pipelineDepth={pipelineDepth?.ToString() ?? "null"}, byteAlignment={byteAlignment?.ToString() ?? "null"}, initialMtuSize={initialMtuSize?.ToString() ?? "null"}, windowCapacity={windowCapacity?.ToString() ?? "null"}, memoryAlignment={memoryAlignment?.ToString() ?? "null"})",
                    resource: "File",
                    category: "FileDownloader"
                ));
            }
            
            _nativeFirmwareInstallerProxy.Nickname = "Firmware Installation"; //todo  get this from a parameter 
            var verdict = _nativeFirmwareInstallerProxy.BeginInstallation(
                data: data,
                mode: mode,
                eraseSettings: eraseSettings,
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                initialMtuSize: initialMtuSize,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment,
                estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
            );

            return verdict;
        }

        public void Cancel() => _nativeFirmwareInstallerProxy?.Cancel();
        public void Disconnect() => _nativeFirmwareInstallerProxy?.Disconnect();
        public void CleanupResourcesOfLastUpload() => _nativeFirmwareInstallerProxy?.CleanupResourcesOfLastInstallation();

        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event EventHandler<IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs> _identicalFirmwareCachedOnTargetDeviceDetected;
        private event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> _firmwareUploadProgressPercentageAndDataThroughputChanged;

        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred
        {
            add
            {
                _fatalErrorOccurred -= value;
                _fatalErrorOccurred += value;
            }
            remove => _fatalErrorOccurred -= value;
        }

        public event EventHandler<LogEmittedEventArgs> LogEmitted
        {
            add
            {
                _logEmitted -= value;
                _logEmitted += value;
            }
            remove => _logEmitted -= value;
        }

        public event EventHandler<CancelledEventArgs> Cancelled
        {
            add
            {
                _cancelled -= value;
                _cancelled += value;
            }
            remove => _cancelled -= value;
        }

        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged
        {
            add
            {
                _busyStateChanged -= value;
                _busyStateChanged += value;
            }
            remove => _busyStateChanged -= value;
        }

        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            add
            {
                _stateChanged -= value;
                _stateChanged += value;
            }
            remove => _stateChanged -= value;
        }

        public event EventHandler<IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs> IdenticalFirmwareCachedOnTargetDeviceDetected
        {
            add
            {
                _identicalFirmwareCachedOnTargetDeviceDetected -= value;
                _identicalFirmwareCachedOnTargetDeviceDetected += value;
            }
            remove => _identicalFirmwareCachedOnTargetDeviceDetected -= value;
        }

        public event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> FirmwareUploadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _firmwareUploadProgressPercentageAndDataThroughputChanged -= value;
                _firmwareUploadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _firmwareUploadProgressPercentageAndDataThroughputChanged -= value;
        }

        private const int DefaultGracefulCancellationTimeoutInMs = 2_500;
        public async Task InstallAsync(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null, //    android only
            int? windowCapacity = null, //    android only
            int? memoryAlignment = null, //   android only
            int? pipelineDepth = null, //     ios only
            int? byteAlignment = null, //     ios only
            int timeoutInMs = -1,
            int maxTriesCount = 10,
            int sleepTimeBetweenRetriesInMs = 100,
            int gracefulCancellationTimeoutInMs = 2_500
        )
        {
            if (maxTriesCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTriesCount), maxTriesCount, "The maximum amount of tries must be greater than zero");
            
            gracefulCancellationTimeoutInMs = gracefulCancellationTimeoutInMs >= 0 //we want to ensure that the timeout is always sane
                ? gracefulCancellationTimeoutInMs
                : DefaultGracefulCancellationTimeoutInMs;

            var isCancellationRequested = false;
            var suspiciousTransportFailuresCount = 0;
            var didWarnOnceAboutUnstableConnection = false;
            for (var triesCount = 1; !isCancellationRequested;)
            {
                var taskCompletionSource = new TaskCompletionSourceRCA<bool>(state: false);
                try
                {
                    Cancelled += FirmwareInstaller_Cancelled_;
                    StateChanged += FirmwareInstaller_StateChanged_;
                    FatalErrorOccurred += FirmwareInstaller_FatalErrorOccurred_;

                    var failSafeSettingsToApply = ConnectionSettingsHelpers.GetFailsafeConnectionSettingsIfConnectionProvedToBeUnstable(
                        uploadingNotDownloading: true,
                        triesCount: triesCount,
                        maxTriesCount: maxTriesCount,
                        suspiciousTransportFailuresCount: suspiciousTransportFailuresCount
                    );
                    if (failSafeSettingsToApply != null)
                    {
                        byteAlignment = failSafeSettingsToApply.Value.byteAlignment;
                        pipelineDepth = failSafeSettingsToApply.Value.pipelineDepth;
                        initialMtuSize = failSafeSettingsToApply.Value.initialMtuSize;
                        windowCapacity = failSafeSettingsToApply.Value.windowCapacity;
                        memoryAlignment = failSafeSettingsToApply.Value.memoryAlignment;

                        if (!didWarnOnceAboutUnstableConnection)
                        {
                            didWarnOnceAboutUnstableConnection = true;
                            OnLogEmitted(new LogEmittedEventArgs(
                                level: ELogLevel.Warning,
                                message: $"[FI.IA.010] Attempt#{triesCount}: Connection is too unstable for uploading the firmware to the target device. Subsequent tries will use failsafe parameters on the connection " +
                                         $"just in case it helps (byteAlignment={byteAlignment}, pipelineDepth={pipelineDepth}, initialMtuSize={initialMtuSize}, windowCapacity={windowCapacity}, memoryAlignment={memoryAlignment})",
                                resource: "Firmware",
                                category: "FirmwareInstaller"
                            ));    
                        }
                    }

                    var verdict = BeginInstallation( //00 dont use task.run here for now
                        data: data,
                        hostDeviceModel: hostDeviceModel,
                        hostDeviceManufacturer: hostDeviceManufacturer,

                        mode: mode,
                        eraseSettings: eraseSettings,
                        estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds,
                        
                        pipelineDepth: pipelineDepth, //      ios only
                        byteAlignment: byteAlignment, //      ios only

                        initialMtuSize: initialMtuSize, //    android only
                        windowCapacity: windowCapacity, //    android only
                        memoryAlignment: memoryAlignment //   android only
                    );
                    if (verdict != EFirmwareInstallationVerdict.Success)
                        throw new ArgumentException(verdict.ToString());

                    await taskCompletionSource.WaitAndFossilizeTaskWithOptionalTimeoutAsync(timeoutInMs);
                }
                catch (TimeoutException ex)
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: EFirmwareInstallationState.None, //better not use this.State here because the native call might fail
                        newState: EFirmwareInstallationState.Error
                    ));

                    throw new FirmwareInstallationTimeoutException(timeoutInMs, ex);
                }
                catch (FirmwareInstallationUploadingStageErroredOutException ex) //we only want to retry if the errors are related to the upload part of the process
                {
                    if (++triesCount > maxTriesCount) //order
                        throw new AllFirmwareInstallationAttemptsFailedException(maxTriesCount, innerException: ex);

                    if (_fileUploadProgressEventsCount <= 10)
                    {
                        suspiciousTransportFailuresCount++;
                    }

                    if (sleepTimeBetweenRetriesInMs > 0) //order
                    {
                        await Task.Delay(sleepTimeBetweenRetriesInMs);
                    }

                    continue;
                }
                catch (Exception ex) when (
                    ex is not ArgumentException //10 wops probably missing native lib symbols!
                    && ex is not TimeoutException
                    && ex is not IFirmwareInstallationException //this accounts for both cancellations and installation errors
                )
                {
                    OnStateChanged(new StateChangedEventArgs( //for consistency
                        oldState: EFirmwareInstallationState.None,
                        newState: EFirmwareInstallationState.Error
                    ));

                    throw new FirmwareInstallationInternalErrorException(ex);
                }
                finally
                {
                    Cancelled -= FirmwareInstaller_Cancelled_;
                    StateChanged -= FirmwareInstaller_StateChanged_;
                    FatalErrorOccurred -= FirmwareInstaller_FatalErrorOccurred_;

                    CleanupResourcesOfLastUpload();
                }

                return;

                void FirmwareInstaller_Cancelled_(object sender, CancelledEventArgs ea)
                {
                    taskCompletionSource.TrySetException(new FirmwareInstallationCancelledException());
                }

                void FirmwareInstaller_StateChanged_(object sender, StateChangedEventArgs ea)
                {
                    switch (ea.NewState)
                    {
                        case EFirmwareInstallationState.Complete:
                            taskCompletionSource.TrySetResult(true);
                            return;

                        case EFirmwareInstallationState.Cancelling:
                            if (isCancellationRequested)
                                return;

                            isCancellationRequested = true;

                            Task.Run(async () =>
                            {
                                try
                                {
                                    if (gracefulCancellationTimeoutInMs > 0) //keep this check here to avoid unnecessary task rescheduling
                                    {
                                        await Task.Delay(gracefulCancellationTimeoutInMs);
                                    }

                                    OnCancelled(new CancelledEventArgs()); //00
                                }
                                catch // (Exception ex)
                                {
                                    // ignored
                                }
                            });
                            return;
                    }

                    //00  we first wait to allow the cancellation to be handled by the underlying native code meaning that we should see OnCancelled()
                    //    getting called right above   but if that takes too long we give the killing blow by calling OnCancelled() manually here
                }

                void FirmwareInstaller_FatalErrorOccurred_(object _, FatalErrorOccurredEventArgs ea_)
                {
                    taskCompletionSource.TrySetException(ea_ switch
                    {
                        { GlobalErrorCode: EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied }
                            => new UnauthorizedException(ea_.ErrorMessage, ea_.GlobalErrorCode),

                        { FatalErrorType: EFirmwareInstallerFatalErrorType.FirmwareImageSwapTimeout }
                            => new FirmwareInstallationConfirmationStageTimeoutException(estimatedSwapTimeInMilliseconds, ea_.GlobalErrorCode),

                        { FatalErrorType: EFirmwareInstallerFatalErrorType.FirmwareUploadingErroredOut } or { State: EFirmwareInstallationState.Uploading }
                            => new FirmwareInstallationUploadingStageErroredOutException(ea_.ErrorMessage, ea_.GlobalErrorCode),

                        _ => new FirmwareInstallationErroredOutException($"{ea_.ErrorMessage} (state={ea_.State})", ea_.GlobalErrorCode)
                    });
                }
            }
            
            //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
            //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
            //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
            //
            //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
        }

        void IFirmwareInstallerEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea); //just to make the class unit-test friendly without making the methods public
        void IFirmwareInstallerEventEmittable.OnLogEmitted(LogEmittedEventArgs ea) => OnLogEmitted(ea);
        void IFirmwareInstallerEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFirmwareInstallerEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFirmwareInstallerEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFirmwareInstallerEventEmittable.OnIdenticalFirmwareCachedOnTargetDeviceDetected(IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea) => OnIdenticalFirmwareCachedOnTargetDeviceDetected(ea);
        void IFirmwareInstallerEventEmittable.OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFirmwareUploadProgressPercentageAndDataThroughputChanged(ea);

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        private void OnIdenticalFirmwareCachedOnTargetDeviceDetected(IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea) => _identicalFirmwareCachedOnTargetDeviceDetected?.Invoke(this, ea);

        private void OnStateChanged(StateChangedEventArgs ea)
        {
            try
            {
                switch (ea)
                {
                    case { NewState: EFirmwareInstallationState.Idle }:
                        _fileUploadProgressEventsCount = 0; //its vital to reset the counter here to account for retries
                        break;

                    case { NewState: EFirmwareInstallationState.Testing } when _fileUploadProgressEventsCount <= 1: //works both on ios and android
                        OnIdenticalFirmwareCachedOnTargetDeviceDetected(new(ECachedFirmwareType.CachedButInactive));
                        break;

                    case { NewState: EFirmwareInstallationState.Complete } when _fileUploadProgressEventsCount <= 1: //works both on ios and android
                        OnIdenticalFirmwareCachedOnTargetDeviceDetected(new(ECachedFirmwareType.CachedAndActive));
                        break;
                }
            }
            finally
            {
                _stateChanged?.Invoke(this, ea); //00 must be dead last
            }

            //00  if we raise the state-changed event before the switch statement then the calling environment will unwire the event handlers of
            //    the identical-firmware-cached-on-target-device-detected event before it gets fired and the event will be ignored altogether
        }

        private int _fileUploadProgressEventsCount;
        private void OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
        {
            try
            {
                _fileUploadProgressEventsCount++;
            }
            finally
            {
                _firmwareUploadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);    
            }
        }

        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFirmwareInstallerProxy
        internal class GenericNativeFirmwareInstallerCallbacksProxy : INativeFirmwareInstallerCallbacksProxy
        {
            public IFirmwareInstallerEventEmittable FirmwareInstaller { get; set; }

            public void CancelledAdvertisement()
                => FirmwareInstaller?.OnCancelled(new CancelledEventArgs());

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => FirmwareInstaller?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                ));

            public void StateChangedAdvertisement(EFirmwareInstallationState oldState, EFirmwareInstallationState newState)
                => FirmwareInstaller?.OnStateChanged(new StateChangedEventArgs(
                    newState: newState,
                    oldState: oldState
                ));

            // public void IdenticalFirmwareCachedOnTargetDeviceDetectedAdvertisement(...) //should not be implemented natively   this event is derived from onstatechanged and is not a native event!

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FirmwareInstaller?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, EGlobalErrorCode globalErrorCode)
                => FirmwareInstaller?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(state, fatalErrorType, errorMessage, globalErrorCode));

            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
                => FirmwareInstaller?.OnFirmwareUploadProgressPercentageAndDataThroughputChanged(new FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
        }
    }
}