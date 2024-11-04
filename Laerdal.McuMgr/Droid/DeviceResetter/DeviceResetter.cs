// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.McuMgr.Bindings.Android;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetter.Contracts.Native;

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
            bluetoothDevice = bluetoothDevice ?? throw new ArgumentNullException(nameof(bluetoothDevice));
            
            androidContext ??= Application.Context;
            //androidContext = androidContext ?? throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird"); //impossible

            return new AndroidNativeDeviceResetterAdapterProxy(
                context: androidContext,
                bluetoothDevice: bluetoothDevice,
                deviceResetterCallbacksProxy: new GenericNativeDeviceResetterCallbacksProxy()
            );
        }

        private sealed class AndroidNativeDeviceResetterAdapterProxy : AndroidDeviceResetter, INativeDeviceResetterProxy
        {
            private readonly INativeDeviceResetterCallbacksProxy _deviceResetterCallbacksProxy;

            public IDeviceResetterEventEmittable DeviceResetter //keep this to conform to the interface
            {
                get => _deviceResetterCallbacksProxy!.DeviceResetter;
                set => _deviceResetterCallbacksProxy!.DeviceResetter = value;
            }

            public EDeviceResetterState State => TranslateEAndroidDeviceResetterState(base.State ?? EAndroidDeviceResetterState.None);

            // ReSharper disable once UnusedMember.Local
            private AndroidNativeDeviceResetterAdapterProxy(IntPtr javaReference, JniHandleOwnership transfer)
                : base(javaReference, transfer)
            {
            }

            internal AndroidNativeDeviceResetterAdapterProxy(INativeDeviceResetterCallbacksProxy deviceResetterCallbacksProxy, Context context, BluetoothDevice bluetoothDevice)
                : base(context, bluetoothDevice)
            {
                _deviceResetterCallbacksProxy = deviceResetterCallbacksProxy ?? throw new ArgumentNullException(nameof(deviceResetterCallbacksProxy));
            }

            public EDeviceResetterInitializationVerdict BeginReset()
            {
                return TranslateEAndroidDeviceResetterInitializationVerdict(base.BeginReset());
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage, int globalErrorCode)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage, globalErrorCode);
                
                FatalErrorOccurredAdvertisement(errorMessage, (EGlobalErrorCode) globalErrorCode);
            }
            
            public void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode)
            {
                _deviceResetterCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage, globalErrorCode);
            }

            public override void StateChangedAdvertisement(EAndroidDeviceResetterState oldState, EAndroidDeviceResetterState newState)
            {
                base.StateChangedAdvertisement(oldState: oldState, currentState: newState);

                StateChangedAdvertisement(
                    newState: TranslateEAndroidDeviceResetterState(newState),
                    oldState: TranslateEAndroidDeviceResetterState(oldState)
                );
            }

            //keep this override   it is needed to conform to the interface
            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
            {
                _deviceResetterCallbacksProxy?.StateChangedAdvertisement(
                    oldState: oldState,
                    newState: newState
                );
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

            //keep this override   it is needed to conform to the interface
            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
            {
                _deviceResetterCallbacksProxy?.LogMessageAdvertisement(message, category, level);
            }

            static private EDeviceResetterState TranslateEAndroidDeviceResetterState(EAndroidDeviceResetterState state)
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
                
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value");
            }

            static private EDeviceResetterInitializationVerdict TranslateEAndroidDeviceResetterInitializationVerdict(EAndroidDeviceResetterInitializationVerdict verdict)
            {
                if (verdict == EAndroidDeviceResetterInitializationVerdict.Success)
                {
                    return EDeviceResetterInitializationVerdict.Success;
                }
                
                if (verdict == EAndroidDeviceResetterInitializationVerdict.FailedErrorUponCommencing)
                {
                    return EDeviceResetterInitializationVerdict.FailedErrorUponCommencing;
                }

                if (verdict == EAndroidDeviceResetterInitializationVerdict.FailedOtherResetAlreadyInProgress)
                {
                    return EDeviceResetterInitializationVerdict.FailedOtherResetAlreadyInProgress;
                }
                
                throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unknown enum value");
            }
        }
    }
}
