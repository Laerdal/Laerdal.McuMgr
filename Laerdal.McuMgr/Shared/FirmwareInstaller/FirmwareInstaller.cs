// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;

using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Events;
using Laerdal.McuMgr.FirmwareInstaller.Exceptions;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
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
            IFirmwareInstaller.EFirmwareInstallationMode mode = IFirmwareInstaller.EFirmwareInstallationMode.TestAndConfirm,
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

                var verdict = BeginInstallation(
                    data: data,
                    mode: mode,
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    eraseSettings: eraseSettings,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds
                );
                if (verdict != IFirmwareInstaller.EFirmwareInstallationVerdict.Success)
                    throw new ArgumentException(verdict.ToString());

                _ = timeoutInMs <= 0
                    ? await taskCompletionSource.Task
                    : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutInMs);
            }
            finally
            {
                Cancelled -= FirmwareInstallationAsyncOnCancelled;
                StateChanged -= FirmwareInstallationAsyncOnStateChanged;
                FatalErrorOccurred -= FirmwareInstallationAsyncOnFatalErrorOccurred;
            }

            void FirmwareInstallationAsyncOnCancelled(object sender, CancelledEventArgs ea)
            {
                taskCompletionSource.TrySetException(new FirmwareInstallationCancelledException());
            }

            void FirmwareInstallationAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
            {
                if (ea.NewState == IFirmwareInstaller.EFirmwareInstallationState.Complete)
                {
                    taskCompletionSource.TrySetResult(true);
                    return;
                }
            }

            void FirmwareInstallationAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
            {
                if (string.IsNullOrWhiteSpace(ea.ErrorMessage)) //fw swap timeout error
                {
                    taskCompletionSource.TrySetException(new FirmwareInstallationErroredOutImageSwapTimeoutException());
                    return;
                }
                
                taskCompletionSource.TrySetException(new FirmwareInstallationErroredOutException(ea.ErrorMessage));
            }
        }

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.Invoke(this, ea);
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
        private void OnFirmwareUploadProgressPercentageAndThroughputDataChangedAdvertisement(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => _firmwareUploadProgressPercentageAndDataThroughputChanged?.Invoke(this, ea);
    }
}