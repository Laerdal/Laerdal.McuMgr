// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;
using Laerdal.McuMgr.DeviceResetter.Contracts.Exceptions;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter, IDeviceResetterEventEmitters
    {
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeDeviceResetterProxy
        internal class GenericNativeDeviceResetterCallbacksProxy : INativeDeviceResetterCallbacksProxy
        {
            public IDeviceResetterEventEmitters DeviceResetter { get; set; }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => DeviceResetter.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: "device-resetter"
                ));
            
            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
                => DeviceResetter.OnStateChanged(new StateChangedEventArgs(
                    newState: newState,
                    oldState: oldState
                ));

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => DeviceResetter.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
        }

        private readonly INativeDeviceResetterProxy _nativeDeviceResetterProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeDeviceResetterProxy
        internal DeviceResetter(INativeDeviceResetterProxy nativeDeviceResetterProxy)
        {
            _nativeDeviceResetterProxy = nativeDeviceResetterProxy ?? throw new ArgumentNullException(nameof(nativeDeviceResetterProxy));
            _nativeDeviceResetterProxy.DeviceResetter = this; //vital
        }

        public string LastFatalErrorMessage => _nativeDeviceResetterProxy?.LastFatalErrorMessage;
        
        public void Disconnect() => _nativeDeviceResetterProxy?.Disconnect();
        public void BeginReset() => _nativeDeviceResetterProxy?.BeginReset();

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

                BeginReset(); //00 dont use task.run here for now

                _ = timeoutInMs <= 0
                    ? await taskCompletionSource.Task
                    : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutInMs);
            }
            catch (TimeoutException)
            {
                (this as IDeviceResetterEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                    oldState: EDeviceResetterState.None, //better not use this.State here because the native call might fail
                    newState: EDeviceResetterState.Failed
                ));
                
                throw; // todo   better throw our our custom timeout exception
            }
            catch (Exception ex) when (
                !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                && !(ex is TimeoutException)
                && !(ex is DeviceResetterErroredOutException)
            )
            {
                (this as IDeviceResetterEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                    oldState: EDeviceResetterState.None,
                    newState: EDeviceResetterState.Failed
                ));

                //OnFatalErrorOccurred();  //better not   it would be a bit confusing to have the error reported in two different ways
                
                throw new DeviceResetterErroredOutException(ex.Message, innerException: ex);
            }
            finally
            {
                StateChanged -= ResetAsyncOnStateChanged;
                FatalErrorOccurred -= ResetAsyncOnFatalErrorOccurred;
            }

            return;

            void ResetAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
            {
                if (ea.NewState != EDeviceResetterState.Complete)
                    return;
                
                taskCompletionSource.TrySetResult(true);
            }

            void ResetAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
            {
                taskCompletionSource.TrySetException(new DeviceResetterErroredOutException(ea.ErrorMessage)); //generic
            }
            
            //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
            //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
            //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
            //
            //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
        }

        void IDeviceResetterEventEmitters.OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea);
        void IDeviceResetterEventEmitters.OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea);
        void IDeviceResetterEventEmitters.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
    }
}