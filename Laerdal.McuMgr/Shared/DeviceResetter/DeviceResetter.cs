// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Events;
using Laerdal.McuMgr.DeviceResetter.Exceptions;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        private event EventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;

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

        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            add
            {
                _stateChanged -= value;
                _stateChanged += value;
            }
            remove => _stateChanged -= value;
        }

        public async Task ResetAsync(int timeoutInMs = -1)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>(state: false);

            try
            {
                StateChanged += ResetAsyncOnStateChanged;
                FatalErrorOccurred += ResetAsyncOnFatalErrorOccurred;

                BeginReset();

                _ = timeoutInMs <= 0
                    ? await taskCompletionSource.Task
                    : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutInMs);
            }
            catch (Exception ex) when (!(ex is DeviceResetterErroredOutException) && !(ex is TimeoutException)) //00 wops probably missing native lib symbols!
            {
                throw new DeviceResetterErroredOutException(ex.Message, ex);
            }
            finally
            {
                StateChanged -= ResetAsyncOnStateChanged;
                FatalErrorOccurred -= ResetAsyncOnFatalErrorOccurred;
            }

            return;

            void ResetAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
            {
                if (ea.NewState != IDeviceResetter.EDeviceResetterState.Complete)
                    return;
                
                taskCompletionSource.TrySetResult(true);
            }

            void ResetAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
            {
                taskCompletionSource.TrySetException(new DeviceResetterErroredOutException(ea.ErrorMessage)); //generic
            }
            
            //00  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
        }

        // ReSharper disable once UnusedMember.Local
        private void OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        private void OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
    }
}