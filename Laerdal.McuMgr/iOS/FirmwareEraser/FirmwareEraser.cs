// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;

using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
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
                eraserCallbacksProxy: new GenericNativeFirmwareEraserCallbacksProxy()
            );
        }

        // ReSharper disable once InconsistentNaming
        private sealed class IOSNativeFirmwareEraserProxy : IOSListenerForFirmwareEraser, INativeFirmwareEraserProxy
        {
            private readonly IOSFirmwareEraser _nativeFirmwareEraser;
            private readonly INativeFirmwareEraserCallbacksProxy _eraserCallbacksProxy;

            internal IOSNativeFirmwareEraserProxy(GenericNativeFirmwareEraserCallbacksProxy eraserCallbacksProxy, CBPeripheral bluetoothDevice)
            {
                if (bluetoothDevice == null)
                    throw new ArgumentNullException(paramName: nameof(bluetoothDevice));
                
                _eraserCallbacksProxy = eraserCallbacksProxy ?? throw new ArgumentNullException(nameof(eraserCallbacksProxy)); //composition-over-inheritance

                _nativeFirmwareEraser = new IOSFirmwareEraser(cbPeripheral: bluetoothDevice, listener: this); //composition-over-inheritance
            }

            #region INativeFirmwareEraserCommandsProxy
            //we are simply forwarding the commands down to the native world of ios here
            public string LastFatalErrorMessage => _nativeFirmwareEraser?.LastFatalErrorMessage;
            public void Disconnect() => _nativeFirmwareEraser?.Disconnect();
            public void BeginErasure(int imageIndex) => _nativeFirmwareEraser?.BeginErasure(imageIndex);
            #endregion

            #region INativeFirmwareEraseCallbacksProxy
            public FirmwareEraser GenericFirmwareEraser //keep this to conform to the interface
            {
                get => _eraserCallbacksProxy?.GenericFirmwareEraser;
                set
                {
                    if (_eraserCallbacksProxy == null)
                        return;

                    _eraserCallbacksProxy.GenericFirmwareEraser = value;
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
                => _eraserCallbacksProxy?.LogMessageAdvertisement(
                    level: level,
                    message: message,
                    category: category
                );

            public override void StateChangedAdvertisement(EIOSFirmwareEraserState oldState, EIOSFirmwareEraserState newState)
                => StateChangedAdvertisement(
                    newState: TranslateEIOSFirmwareEraserState(newState),
                    oldState: TranslateEIOSFirmwareEraserState(oldState)
                );
            public void StateChangedAdvertisement(IFirmwareEraser.EFirmwareErasureState oldState, IFirmwareEraser.EFirmwareErasureState newState) //keep this to conform to the interface
                => _eraserCallbacksProxy?.StateChangedAdvertisement(
                    newState: newState,
                    oldState: oldState
                );

            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _eraserCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            public override void FatalErrorOccurredAdvertisement(string errorMessage) => _eraserCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage);
            #endregion

            // ReSharper disable once InconsistentNaming
            static private IFirmwareEraser.EFirmwareErasureState TranslateEIOSFirmwareEraserState(EIOSFirmwareEraserState state) => state switch
            {
                EIOSFirmwareEraserState.None => IFirmwareEraser.EFirmwareErasureState.None,
                EIOSFirmwareEraserState.Idle => IFirmwareEraser.EFirmwareErasureState.Idle,
                EIOSFirmwareEraserState.Erasing => IFirmwareEraser.EFirmwareErasureState.Erasing,
                EIOSFirmwareEraserState.Complete => IFirmwareEraser.EFirmwareErasureState.Complete,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}