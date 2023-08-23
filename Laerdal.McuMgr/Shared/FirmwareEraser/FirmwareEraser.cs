// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Events;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Exceptions;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Native;

[assembly: InternalsVisibleTo("Laerdal.McuMgr.Tests")]
namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser, IFirmwareEraserEventEmitters
    {
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFirmwareEraserProxy
        internal class GenericNativeFirmwareEraserCallbacksProxy : INativeFirmwareEraserCallbacksProxy
        {
            public IFirmwareEraserEventEmitters FirmwareEraser { get; set; }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => FirmwareEraser.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: "firmware-eraser"
                ));
            
            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState)
                => FirmwareEraser.OnStateChanged(new StateChangedEventArgs(
                    newState: newState,
                    oldState: oldState
                ));

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FirmwareEraser.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => FirmwareEraser.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
        }
        
        private readonly INativeFirmwareEraserProxy _nativeFirmwareEraserProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFirmwareEraserProxy
        internal FirmwareEraser(INativeFirmwareEraserProxy nativeFirmwareEraserProxy)
        {
            _nativeFirmwareEraserProxy = nativeFirmwareEraserProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareEraserProxy));
            _nativeFirmwareEraserProxy.FirmwareEraser = this; //vital
        }
        
        public string LastFatalErrorMessage => _nativeFirmwareEraserProxy?.LastFatalErrorMessage;

        public void Disconnect() => _nativeFirmwareEraserProxy?.Disconnect();
        public void BeginErasure(int imageIndex = 1) => _nativeFirmwareEraserProxy?.BeginErasure(imageIndex);
        
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

                BeginErasure(imageIndex); //00 dont use task.run here for now

                _ = timeoutInMs <= 0
                    ? await taskCompletionSource.Task
                    : await taskCompletionSource.Task.WithTimeoutInMs(timeout: timeoutInMs);
            }
            catch (TimeoutException ex)
            {
                (this as IFirmwareEraserEventEmitters).OnStateChanged(new StateChangedEventArgs( //for consistency
                    oldState: EFirmwareErasureState.None, //better not use this.State here because the native call might fail
                    newState: EFirmwareErasureState.Failed
                ));

                throw new FirmwareErasureTimeoutException(timeoutInMs, ex);
            }
            catch (Exception ex) when (
                !(ex is ArgumentException) //10 wops probably missing native lib symbols!
                && !(ex is TimeoutException)
                && !(ex is FirmwareErasureErroredOutException)
            )
            {
                throw new FirmwareErasureErroredOutException(ex.Message, ex);
            }
            finally
            {
                StateChanged -= EraseAsyncOnStateChanged;
                FatalErrorOccurred -= EraseAsyncOnFatalErrorOccurred;
            }

            return;

            void EraseAsyncOnStateChanged(object sender, StateChangedEventArgs ea)
            {
                if (ea.NewState != EFirmwareErasureState.Complete)
                    return;

                taskCompletionSource.TrySetResult(true);
            }

            void EraseAsyncOnFatalErrorOccurred(object sender, FatalErrorOccurredEventArgs ea)
            {
                taskCompletionSource.TrySetException(new FirmwareErasureErroredOutException(ea.ErrorMessage)); //generic
            }
            
            //00  we are aware that in order to be 100% accurate about timeouts we should use task.run() here without await and then await the
            //    taskcompletionsource right after    but if we went down this path we would also have to account for exceptions thus complicating
            //    the code considerably for little to no practical gain considering that the native call has trivial setup code and is very fast
            //
            //10  we dont want to wrap our own exceptions obviously   we only want to sanitize native exceptions from java and swift that stem
            //    from missing libraries and symbols because we dont want the raw native exceptions to bubble up to the managed code
        }

        void IFirmwareEraserEventEmitters.OnLogEmitted(LogEmittedEventArgs ea) => _logEmitted?.Invoke(this, ea); //       we made these interface implementations
        void IFirmwareEraserEventEmitters.OnStateChanged(StateChangedEventArgs ea) => _stateChanged?.Invoke(this, ea); // explicit to avoid making them public
        void IFirmwareEraserEventEmitters.OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.Invoke(this, ea);
        void IFirmwareEraserEventEmitters.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => _fatalErrorOccurred?.Invoke(this, ea);
    }
}