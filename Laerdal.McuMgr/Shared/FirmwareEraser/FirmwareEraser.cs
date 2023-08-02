// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;

using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Events;
using Laerdal.McuMgr.FirmwareEraser.Exceptions;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;

        public event EventHandler<LogEmittedEventArgs> LogEmitted
        {
            add
            {
                _logEmitted -= value;
                _logEmitted += value;
            }
            remove => _logEmitted -= value;
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

        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged
        {
            add
            {
                _busyStateChanged -= value;
                _busyStateChanged += value;
            }
            remove => _busyStateChanged -= value;
        }

        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred
        {
            add
            {
                _fatalErrorOccurred -= value;
                _fatalErrorOccurred += value;
            }
            remove => _fatalErrorOccurred -= value;
        }

        public async Task EraseAsync(int imageIndex = 1, int timeoutInMs = -1)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>(state: false);

            try
            {
                StateChanged += EraseAsyncOnStateChanged;
                FatalErrorOccurred += EraseAsyncOnFatalErrorOccurred;

                BeginErasure(imageIndex);

                _ = timeoutInMs <= 0
                    ? await taskCompletionSource.Task
                    : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutInMs);
            }
            finally
            {
                StateChanged -= EraseAsyncOnStateChanged;
                FatalErrorOccurred -= EraseAsyncOnFatalErrorOccurred;
            }

            void EraseAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
            {
                if (ea.NewState != IFirmwareEraser.EFirmwareErasureState.Complete)
                    return;

                taskCompletionSource.TrySetResult(true);
            }

            void EraseAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
            {
                taskCompletionSource.TrySetException(new FirmwareErasureErroredOutException(ea.ErrorMessage)); //generic
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
    }
}