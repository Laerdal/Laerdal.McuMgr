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
        internal interface INativeFirmwareEraserProxy : INativeFirmwareEraserCommandsProxy, INativeFirmwareEraserCallbacksProxy
        {
        }

        internal interface INativeFirmwareEraserCommandsProxy
        {
            // ReSharper disable UnusedMember.Global
            string LastFatalErrorMessage { get; }

            void Disconnect();
            void BeginErasure(int imageIndex);
        }

        internal interface INativeFirmwareEraserCallbacksProxy
        {
            FirmwareEraser GenericFirmwareEraser { get; set; }
            
            void LogMessageAdvertisement(string message, string category, ELogLevel level);
            void StateChangedAdvertisement(IFirmwareEraser.EFirmwareErasureState oldState, IFirmwareEraser.EFirmwareErasureState newState);
            void BusyStateChangedAdvertisement(bool busyNotIdle);
            void FatalErrorOccurredAdvertisement(string errorMessage);
        }

        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFirmwareEraserProxy
        internal class GenericNativeFirmwareEraserCallbacksProxy : INativeFirmwareEraserCallbacksProxy
        {
            public FirmwareEraser GenericFirmwareEraser { get; set; }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => GenericFirmwareEraser.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: "firmware-eraser"
                ));
            
            public void StateChangedAdvertisement(IFirmwareEraser.EFirmwareErasureState oldState, IFirmwareEraser.EFirmwareErasureState newState)
                => GenericFirmwareEraser.OnStateChanged(new StateChangedEventArgs(
                    newState: newState,
                    oldState: oldState
                ));

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => GenericFirmwareEraser.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => GenericFirmwareEraser.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
        }
        
        private readonly INativeFirmwareEraserProxy _nativeFirmwareEraserProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFirmwareEraserProxy
        internal FirmwareEraser(INativeFirmwareEraserProxy nativeFirmwareEraserProxy = null)
        {
            _nativeFirmwareEraserProxy = nativeFirmwareEraserProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareEraserProxy));
            _nativeFirmwareEraserProxy.GenericFirmwareEraser = this; //vital
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