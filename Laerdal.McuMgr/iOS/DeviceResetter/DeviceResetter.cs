// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetter.Contracts.Native;
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
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeDeviceResetterProxy(
                bluetoothDevice: bluetoothDevice,
                nativeResetterCallbacksProxy: new GenericNativeDeviceResetterCallbacksProxy()
            );
        }

        // ReSharper disable once InconsistentNaming
        private sealed class IOSNativeDeviceResetterProxy : IOSListenerForDeviceResetter, INativeDeviceResetterProxy
        {
            private readonly IOSDeviceResetter _nativeIosDeviceResetter;
            private readonly INativeDeviceResetterCallbacksProxy _nativeResetterCallbacksProxy;

            internal IOSNativeDeviceResetterProxy(CBPeripheral bluetoothDevice, INativeDeviceResetterCallbacksProxy nativeResetterCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeResetterCallbacksProxy = nativeResetterCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeResetterCallbacksProxy));

                _nativeIosDeviceResetter = new IOSDeviceResetter(listener: this, cbPeripheral: bluetoothDevice);
                _nativeResetterCallbacksProxy = nativeResetterCallbacksProxy; //composition-over-inheritance
            }

            #region commands
            
            public EDeviceResetterState State => TranslateEIOSDeviceResetterState(_nativeIosDeviceResetter?.State ?? EIOSDeviceResetterState.None);
            public string LastFatalErrorMessage => _nativeIosDeviceResetter?.LastFatalErrorMessage;

            public void Disconnect() => _nativeIosDeviceResetter?.Disconnect();
            public void BeginReset() => _nativeIosDeviceResetter?.BeginReset();

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
            
            #endregion



            #region listener callbacks -> event emitters
            
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

            #endregion listener events

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