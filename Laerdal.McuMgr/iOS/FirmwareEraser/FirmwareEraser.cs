// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Native;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        public FirmwareEraser(CBPeripheral bluetoothDevice) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice))
        {
        }
        
        static private INativeFirmwareEraserProxy ValidateArgumentsAndConstructProxy(CBPeripheral bluetoothDevice)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            return new IOSNativeFirmwareEraserProxy(
                bluetoothDevice: bluetoothDevice,
                nativeFirmwareEraserCallbacksProxy: new GenericNativeFirmwareEraserCallbacksProxy()
            );
        }

        // ReSharper disable once InconsistentNaming
        private sealed class IOSNativeFirmwareEraserProxy : IOSListenerForFirmwareEraser, INativeFirmwareEraserProxy
        {
            private readonly IOSFirmwareEraser _firmwareEraser;
            private readonly INativeFirmwareEraserCallbacksProxy _nativeFirmwareEraserCallbacksProxy;

            internal IOSNativeFirmwareEraserProxy(CBPeripheral bluetoothDevice, INativeFirmwareEraserCallbacksProxy nativeFirmwareEraserCallbacksProxy)
            {
                bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
                nativeFirmwareEraserCallbacksProxy = nativeFirmwareEraserCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareEraserCallbacksProxy));

                _firmwareEraser = new IOSFirmwareEraser(listener: this, cbPeripheral: bluetoothDevice);
                _nativeFirmwareEraserCallbacksProxy = nativeFirmwareEraserCallbacksProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareEraserCallbacksProxy)); //composition-over-inheritance
            }

            #region INativeFirmwareEraserCommandsProxy
            //we are simply forwarding the commands down to the native world of ios here
            public string LastFatalErrorMessage => _firmwareEraser?.LastFatalErrorMessage;
            public void Disconnect() => _firmwareEraser?.Disconnect();
            public void BeginErasure(int imageIndex) => _firmwareEraser?.BeginErasure(imageIndex);
            #endregion

            #region INativeFirmwareEraseCallbacksProxy
            public IFirmwareEraserEventEmittable FirmwareEraser //keep this to conform to the interface
            {
                get => _nativeFirmwareEraserCallbacksProxy?.FirmwareEraser;
                set
                {
                    if (_nativeFirmwareEraserCallbacksProxy == null)
                        return;

                    _nativeFirmwareEraserCallbacksProxy.FirmwareEraser = value;
                }
            }

            //we are simply forwarding the calls up towards the surface world of csharp here
            public override void LogMessageAdvertisement(string message, string category, string level)
                => LogMessageAdvertisement(
                    level: HelpersIOS.TranslateEIOSLogLevel(level),
                    message: message,
                    category: category
                );
            
            public void LogMessageAdvertisement(string message, string category, ELogLevel level) //keep this to conform to the interface
                => _nativeFirmwareEraserCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category
                );

            public override void StateChangedAdvertisement(EIOSFirmwareEraserState oldState, EIOSFirmwareEraserState newState)
                => StateChangedAdvertisement(
                    newState: TranslateEIOSFirmwareEraserState(newState),
                    oldState: TranslateEIOSFirmwareEraserState(oldState)
                );
            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState) //keep this to conform to the interface
                => _nativeFirmwareEraserCallbacksProxy?.StateChangedAdvertisement(
                    newState: newState,
                    oldState: oldState
                );

            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _nativeFirmwareEraserCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            public override void FatalErrorOccurredAdvertisement(string errorMessage) => _nativeFirmwareEraserCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage);
            #endregion

            // ReSharper disable once InconsistentNaming
            static private EFirmwareErasureState TranslateEIOSFirmwareEraserState(EIOSFirmwareEraserState state) => state switch
            {
                EIOSFirmwareEraserState.None => EFirmwareErasureState.None,
                EIOSFirmwareEraserState.Idle => EFirmwareErasureState.Idle,
                EIOSFirmwareEraserState.Erasing => EFirmwareErasureState.Erasing,
                EIOSFirmwareEraserState.Complete => EFirmwareErasureState.Complete,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}