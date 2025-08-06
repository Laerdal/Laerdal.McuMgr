// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Exceptions;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.DeviceResetting.Contracts;
using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetting.Contracts.Events;
using Laerdal.McuMgr.DeviceResetting.Contracts.Exceptions;
using Laerdal.McuMgr.DeviceResetting.Contracts.Native;

namespace Laerdal.McuMgr.DeviceResetting
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter, IDeviceResetterEventEmittable
    {
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeDeviceResetterProxy
        internal class GenericNativeDeviceResetterCallbacksProxy : INativeDeviceResetterCallbacksProxy
        {
            public IDeviceResetterEventEmittable DeviceResetter { get; set; }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => DeviceResetter?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: "device-resetter"
                ));
            
            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
                => DeviceResetter?.OnStateChanged(new StateChangedEventArgs(
                    newState: newState,
                    oldState: oldState
                ));

            public void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode)
                => DeviceResetter?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage, globalErrorCode));
        }

        private readonly INativeDeviceResetterProxy _nativeDeviceResetterProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeDeviceResetterProxy
        internal DeviceResetter(INativeDeviceResetterProxy nativeDeviceResetterProxy)
        {
            _nativeDeviceResetterProxy = nativeDeviceResetterProxy ?? throw new ArgumentNullException(nameof(nativeDeviceResetterProxy));
            _nativeDeviceResetterProxy.DeviceResetter = this; //vital
        }

        public EDeviceResetterState State => _nativeDeviceResetterProxy?.State ?? EDeviceResetterState.None;
        public string LastFatalErrorMessage => _nativeDeviceResetterProxy?.LastFatalErrorMessage;

        public void Disconnect() => _nativeDeviceResetterProxy?.Disconnect();
        public EDeviceResetterInitializationVerdict BeginReset()
        {
            if (_nativeDeviceResetterProxy == null)
                throw new InvalidOperationException("The native device resetter is not initialized");
            
            return _nativeDeviceResetterProxy.BeginReset();
        }

        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> _logEmitted;

        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred
        {
            add
            {
                _fatalErrorOccurred -= value;
                _fatalErrorOccurred += value;
            }
            remove => _fatalErrorOccurred -= value;
        }

        public event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted
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
            var taskCompletionSource = new TaskCompletionSourceRCA<bool>(state: false);

            try
            {
                StateChanged += DeviceResetter_StateChanged_;
                FatalErrorOccurred += DeviceResetter_FatalErrorOccurred_;

                var verdict = BeginReset(); //00 dont use task.run here for now
                if (verdict != EDeviceResetterInitializationVerdict.Success)
                    throw new ArgumentException(verdict.ToString());

                await taskCompletionSource.WaitAndFossilizeTaskOnOptionalTimeoutAsync(timeoutInMs);
            }
            catch (TimeoutException ex)
            {
                (this as IDeviceResetterEventEmittable).OnStateChanged(new StateChangedEventArgs( //for consistency
                    oldState: EDeviceResetterState.None, //better not use this.State here because the native call might fail
                    newState: EDeviceResetterState.Failed
                ));

                throw new DeviceResetTimeoutException(timeoutInMs, ex);
            }
            catch (Exception ex) when (
                ex is not ArgumentException //10 wops probably missing native lib symbols!
                && ex is not TimeoutException
                && !(ex is IDeviceResetterException)
            )
            {
                (this as IDeviceResetterEventEmittable).OnStateChanged(new StateChangedEventArgs( //for consistency
                    oldState: EDeviceResetterState.None,
                    newState: EDeviceResetterState.Failed
                ));

                //OnFatalErrorOccurred();  //better not   it would be a bit confusing to have the error reported in two different ways
                
                throw new DeviceResetterInternalErrorException(ex);
            }
            finally
            {
                StateChanged -= DeviceResetter_StateChanged_;
                FatalErrorOccurred -= DeviceResetter_FatalErrorOccurred_;
            }

            return;

            void DeviceResetter_StateChanged_(object _, StateChangedEventArgs ea_)
            {
                if (ea_.NewState != EDeviceResetterState.Complete)
                    return;
                
                taskCompletionSource.TrySetResult(true);
            }

            void DeviceResetter_FatalErrorOccurred_(object _, FatalErrorOccurredEventArgs ea_)
            {
                taskCompletionSource.TrySetException(ea_.GlobalErrorCode switch
                {
                    EGlobalErrorCode.McuMgrErrorBeforeSmpV2_AccessDenied => new UnauthorizedException(ea_.ErrorMessage, ea_.GlobalErrorCode),
                    _ => new DeviceResetterErroredOutException(ea_.ErrorMessage, ea_.GlobalErrorCode)
                });
            }
            
            //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
            //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
            //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
            //
            //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
        }

        void ILogEmittable.OnLogEmitted(in LogEmittedEventArgs ea) => _logEmitted?.InvokeAndIgnoreExceptions(this, in ea); // in the special case of log-emitted we prefer the .invoke() flavour for the sake of performance
        void IDeviceResetterEventEmittable.OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        void IDeviceResetterEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea)
        {            
            (this as ILogEmittable).OnLogEmitted(new LogEmittedEventArgs(
                level: ELogLevel.Error,
                message: $"[{nameof(ea.GlobalErrorCode)}='{ea.GlobalErrorCode}'] {ea.ErrorMessage}",
                resource: "",
                category: "device-resetter"
            ));
            
            OnFatalErrorOccurred_(ea);
            return;

            void OnFatalErrorOccurred_(FatalErrorOccurredEventArgs ea_)
            {
                _fatalErrorOccurred?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea_);
            }
        }
    }
}