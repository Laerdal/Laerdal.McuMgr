// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        public DeviceResetter(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }

        static private INativeDeviceResetterProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeDeviceResetterProxy(
                bluetoothDevice: bluetoothDevice,
                deviceResetterCallbacksProxy: new GenericNativeDeviceResetterCallbacksProxy()
            );
        }

        public EDeviceResetterState State => IOSNativeDeviceResetterProxy.TranslateEIOSDeviceResetterState(
            (EIOSDeviceResetterState)(_nativeDeviceResetterProxy?.State ?? EIOSDeviceResetterState.None)
        );

        // ReSharper disable once InconsistentNaming
        private sealed class IOSNativeDeviceResetterProxy : IOSListenerForDeviceResetter, INativeDeviceResetterProxy
        {
            private readonly IOSDeviceResetter _deviceResetter;
            private readonly INativeDeviceResetterCallbacksProxy _nativeResetterCallbacksProxy;

            internal IOSNativeDeviceResetterProxy(INativeDeviceResetterCallbacksProxy deviceResetterCallbacksProxy, CBPeripheral bluetoothDevice)
            {
                if (bluetoothDevice == null)
                    throw new ArgumentNullException(paramName: nameof(bluetoothDevice));
                
                _nativeResetterCallbacksProxy = deviceResetterCallbacksProxy ?? throw new ArgumentNullException(nameof(deviceResetterCallbacksProxy)); //composition-over-inheritance

                _deviceResetter = new IOSDeviceResetter(cbPeripheral: bluetoothDevice, listener: this); //composition-over-inheritance
            }

            public object State => _deviceResetter?.State;
            public string LastFatalErrorMessage => _deviceResetter?.LastFatalErrorMessage;
            public void Disconnect() => _deviceResetter.Disconnect();
            public void BeginReset() => _deviceResetter.BeginReset();
            
            public IDeviceResetterEventEmitters DeviceResetter //keep this to conform to the interface
            {
                get => _nativeResetterCallbacksProxy?.DeviceResetter;
                set
                {
                    if (_nativeResetterCallbacksProxy == null)
                        return;

                    _nativeResetterCallbacksProxy.DeviceResetter = value;
                }
            }

            //we are simply forwarding the calls up towards the surface world of csharp here
            public override void LogMessageAdvertisement(string message, string category, string level)
                => LogMessageAdvertisement(
                    level: HelpersIOS.TranslateEIOSLogLevel(level),
                    message: message,
                    category: category
                );
            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _nativeResetterCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category
                );

            public override void StateChangedAdvertisement(EIOSDeviceResetterState oldState, EIOSDeviceResetterState newState)
                => StateChangedAdvertisement(
                    newState: TranslateEIOSDeviceResetterState(newState),
                    oldState: TranslateEIOSDeviceResetterState(oldState)
                );
            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
                => _nativeResetterCallbacksProxy?.StateChangedAdvertisement(
                    newState: newState,
                    oldState: oldState
                );

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
                => _nativeResetterCallbacksProxy?.FatalErrorOccurredAdvertisement(
                    errorMessage: errorMessage
                );

            // ReSharper disable once InconsistentNaming
            static public EDeviceResetterState TranslateEIOSDeviceResetterState(EIOSDeviceResetterState state) => state switch
            {
                EIOSDeviceResetterState.None => EDeviceResetterState.None,
                EIOSDeviceResetterState.Idle => EDeviceResetterState.Idle,
                EIOSDeviceResetterState.Failed => EDeviceResetterState.Failed,
                EIOSDeviceResetterState.Complete => EDeviceResetterState.Complete,
                EIOSDeviceResetterState.Resetting => EDeviceResetterState.Resetting,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}