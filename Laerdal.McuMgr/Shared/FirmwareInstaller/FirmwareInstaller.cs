// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
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

        public EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? windowCapacity = null, //   android only    not applicable for ios
            int? memoryAlignment = null, //  android only    not applicable for ios
            int? pipelineDepth = null, //    ios only        not applicable for android
            int? byteAlignment = null //     ios only        not applicable for android
        )
        {
            if (data == null || !data.Any())
                throw new ArgumentException("The data byte-array parameter is null or empty", nameof(data));

            _nativeFirmwareInstallerProxy.Nickname = "Firmware Installation"; //todo  get this from a parameter 
            var verdict = _nativeFirmwareInstallerProxy.BeginInstallation(
                data: data,
                mode: mode,
                eraseSettings: eraseSettings ?? false,
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                windowCapacity: windowCapacity ?? -1,
                memoryAlignment: memoryAlignment ?? -1,
                estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds ?? -1
            );

            return verdict;
        }
        
        public void Cancel() => _nativeFirmwareInstallerProxy?.Cancel();
        public void Disconnect() => _nativeFirmwareInstallerProxy?.Disconnect();

        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
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

        public event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> FirmwareUploadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _firmwareUploadProgressPercentageAndDataThroughputChanged -= value;
                _firmwareUploadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _firmwareUploadProgressPercentageAndDataThroughputChanged -= value;
        }

        public async Task InstallAsync(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? windowCapacity = null,
            int? memoryAlignment = null,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int timeoutInMs = -1
        )
        {
            var taskCompletionSource = new TaskCompletionSource<bool>(state: false);

            try
            {
                Cancelled += FirmwareInstallationAsyncOnCancelled;
                StateChanged += FirmwareInstallationAsyncOnStateChanged;
                FatalErrorOccurred += FirmwareInstallationAsyncOnFatalErrorOccurred;

                var verdict = BeginInstallation( //00 dont use task.run here for now
                    data: data,
                    mode: mode,
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    eraseSettings: eraseSettings,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
                );
                if (verdict != EFirmwareInstallationVerdict.Success)
                    throw new ArgumentException(verdict.ToString());

                _ = timeoutInMs <= 0
                    ? await taskCompletionSource.Task
                    : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutInMs);
            }
            catch (TimeoutException ex)
            {
                OnStateChanged(new StateChangedEventArgs( //for consistency
                    oldState: EFirmwareInstallationState.None, //better not use this.State here because the native call might fail
                    newState: EFirmwareInstallationState.Error
                ));

                throw new FirmwareInstallationTimeoutException(timeoutInMs, ex);
            }
            catch (Exception ex) when (
                !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                && !(ex is TimeoutException)
                && !(ex is IFirmwareInstallationException) //this accounts for both cancellations and installation errors
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
                Cancelled -= FirmwareInstallationAsyncOnCancelled;
                StateChanged -= FirmwareInstallationAsyncOnStateChanged;
                FatalErrorOccurred -= FirmwareInstallationAsyncOnFatalErrorOccurred;
            }

            return;

            void FirmwareInstallationAsyncOnCancelled(object sender, CancelledEventArgs ea)
            {
                taskCompletionSource.TrySetException(new FirmwareInstallationCancelledException());
            }

            void FirmwareInstallationAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
            {
                if (ea.NewState != EFirmwareInstallationState.Complete)
                    return;

                taskCompletionSource.TrySetResult(true);
            }

            void FirmwareInstallationAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
            {
                if (string.IsNullOrWhiteSpace(ea.ErrorMessage)) //fw swap timeout error   todo  we should also take into account the state the installation is in
                {
                    taskCompletionSource.TrySetException(new FirmwareInstallationErroredOutImageSwapTimeoutException());
                    return;
                }

                taskCompletionSource.TrySetException(new FirmwareInstallationErroredOutException(ea.ErrorMessage));
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
        void IFirmwareInstallerEventEmittable.OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFirmwareUploadProgressPercentageAndDataThroughputChanged(ea);
        
        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        private void OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _firmwareUploadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);

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

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FirmwareInstaller?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => FirmwareInstaller?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
            
            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
                => FirmwareInstaller?.OnFirmwareUploadProgressPercentageAndDataThroughputChanged(new FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(
                    averageThroughput: averageThroughput,
                    progressPercentage: progressPercentage
                ));
        }
    }
}