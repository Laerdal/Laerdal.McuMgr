// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        public DeviceResetter(BluetoothDevice bluetoothDevice, Context androidContext = null) : this(ValidateArgumentsAndConstructProxy(bluetoothDevice, androidContext))
        {
        }
        
        static private INativeDeviceResetterProxy ValidateArgumentsAndConstructProxy(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            return new AndroidNativeDeviceResetterAdapterProxy(new GenericNativeDeviceResetterCallbacksProxy(), androidContext, bluetoothDevice);
        }

        public EDeviceResetterState State => AndroidNativeDeviceResetterAdapterProxy.TranslateEAndroidDeviceResetterState(
            (EAndroidDeviceResetterState)(_nativeDeviceResetterProxy?.State ?? EAndroidDeviceResetterState.None)
        );

        private sealed class AndroidNativeDeviceResetterAdapterProxy : AndroidDeviceResetter, INativeDeviceResetterProxy
        {
            private readonly INativeDeviceResetterCallbacksProxy _deviceResetterCallbacksProxy;

            public IDeviceResetterEventEmitters DeviceResetter //keep this to conform to the interface
            {
                get => _deviceResetterCallbacksProxy?.DeviceResetter;
                set
                {
                    if (_deviceResetterCallbacksProxy == null)
                        return;

                    _deviceResetterCallbacksProxy.DeviceResetter = value;
                }
            }

            public new object State => base.State; //we convert the native state to a mere object because otherwise we would have to resort to generics 

            // ReSharper disable once UnusedMember.Local
            private AndroidNativeDeviceResetterAdapterProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidNativeDeviceResetterAdapterProxy(INativeDeviceResetterCallbacksProxy deviceResetterCallbacksProxy, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _deviceResetterCallbacksProxy = deviceResetterCallbacksProxy ?? throw new ArgumentNullException(nameof(deviceResetterCallbacksProxy));
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage);
                
                _deviceResetterCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage);
            }

            public override void StateChangedAdvertisement(EAndroidDeviceResetterState oldState, EAndroidDeviceResetterState newState)
            {
                base.StateChangedAdvertisement(oldState: oldState, currentState: newState);

                StateChangedAdvertisement(
                    newState: TranslateEAndroidDeviceResetterState(newState),
                    oldState: TranslateEAndroidDeviceResetterState(oldState)
                );
            }

            //keep this method to adhere to the interface
            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
            {
                _deviceResetterCallbacksProxy.StateChangedAdvertisement(newState, oldState);
            }
            
            // todo   implement in this android too
            // public override void LogMessageAdvertisement(string message, string category, string level)
            // {
            //     base.LogMessageAdvertisement(message, category, level);
            //
            //     LogMessageAdvertisement(
            //         level: HelpersAndroid.TranslateEAndroidLogLevel(level),
            //         message: message,
            //         category: category
            //     );
            // }

            //keep this override   its needed to conform to the interface
            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
            {
                _deviceResetterCallbacksProxy.LogMessageAdvertisement(message, category, level);
            }

            static public EDeviceResetterState TranslateEAndroidDeviceResetterState(EAndroidDeviceResetterState state)
            {
                if (state == EAndroidDeviceResetterState.None)
                {
                    return EDeviceResetterState.None;
                }
                
                if (state == EAndroidDeviceResetterState.Idle)
                {
                    return EDeviceResetterState.Idle;
                }

                if (state == EAndroidDeviceResetterState.Resetting)
                {
                    return EDeviceResetterState.Resetting;
                }

                if (state == EAndroidDeviceResetterState.Complete)
                {
                    return EDeviceResetterState.Complete;
                }
                
                if (state == EAndroidDeviceResetterState.Failed)
                {
                    return EDeviceResetterState.Failed;
                }
                
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
