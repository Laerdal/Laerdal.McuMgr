// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;

using Laerdal.Java.McuMgr.Wrapper.Android;

using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        public FirmwareEraser(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        { 
        }

        static private INativeFirmwareEraserProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            return new AndroidNativeFirmwareEraserAdapterProxy(
                androidContext: androidContext,
                bluetoothDevice: bluetoothDevice,
                eraserCallbacksProxy: new GenericNativeFirmwareEraserCallbacksProxy()
            );
        }

        internal sealed class AndroidNativeFirmwareEraserAdapterProxy : AndroidFirmwareEraser, INativeFirmwareEraserProxy
        {
            private readonly INativeFirmwareEraserCallbacksProxy _eraserCallbacksProxy;

            // ReSharper disable once UnusedMember.Local
            private AndroidNativeFirmwareEraserAdapterProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidNativeFirmwareEraserAdapterProxy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy, Context androidContext, BluetoothDevice bluetoothDevice)
                : base(androidContext, bluetoothDevice)
            {
                if (bluetoothDevice == null)
                    throw new ArgumentNullException(nameof(bluetoothDevice));

                androidContext ??= Application.Context;
                if (androidContext == null)
                    throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");
                
                _eraserCallbacksProxy = eraserCallbacksProxy ?? throw new ArgumentNullException(nameof(eraserCallbacksProxy)); //composition-over-inheritance
            }
            
            public IFirmwareEraserEventEmitters FirmwareEraser //keep this to conform to the interface
            {
                get => _eraserCallbacksProxy?.FirmwareEraser;
                set
                {
                    if (_eraserCallbacksProxy == null)
                        return;

                    _eraserCallbacksProxy.FirmwareEraser = value;
                }
            }

            public override void StateChangedAdvertisement(EAndroidFirmwareEraserState oldState, EAndroidFirmwareEraserState newState)
            {
                base.StateChangedAdvertisement(oldState, newState);
                
                StateChangedAdvertisement(newState: TranslateEAndroidFirmwareEraserState(newState), oldState: TranslateEAndroidFirmwareEraserState(oldState));
            }
            
            //keep this override   its needed to conform to the interface
            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState)
            {
                _eraserCallbacksProxy.StateChangedAdvertisement(newState: newState, oldState: oldState);
            }
            
            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case

                _eraserCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage);
                
                _eraserCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage);
            }
            
            public override void LogMessageAdvertisement(string message, string category, string level)
            {
                base.LogMessageAdvertisement(message, category, level);

                LogMessageAdvertisement(
                    level: HelpersAndroid.TranslateEAndroidLogLevel(level),
                    message: message,
                    category: category
                );
            }

            //keep this override   its needed to conform to the interface
            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
            {
                _eraserCallbacksProxy.LogMessageAdvertisement(message, category, level);
            }

            // ReSharper disable once MemberCanBePrivate.Global
            static internal EFirmwareErasureState TranslateEAndroidFirmwareEraserState(EAndroidFirmwareEraserState state)
            {
                if (state == EAndroidFirmwareEraserState.None)
                {
                    return EFirmwareErasureState.None;
                }
                
                if (state == EAndroidFirmwareEraserState.Idle)
                {
                    return EFirmwareErasureState.Idle;
                }

                if (state == EAndroidFirmwareEraserState.Erasing)
                {
                    return EFirmwareErasureState.Erasing;
                }

                if (state == EAndroidFirmwareEraserState.Complete)
                {
                    return EFirmwareErasureState.Complete;
                }
                
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
