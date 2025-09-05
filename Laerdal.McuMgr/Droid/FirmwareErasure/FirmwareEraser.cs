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
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareErasure.Contracts;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareErasure
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        public FirmwareEraser(object nativeBluetoothDevice, object androidContext = null) : this( // platform independent utility constructor to make life easier in terms of qol/dx in MAUI
            androidContext: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<Context>(obj: androidContext, parameterName: nameof(androidContext), allowNulls: true),
            bluetoothDevice: NativeBluetoothDeviceHelpers.EnsureObjectIsCastableToType<BluetoothDevice>(obj: nativeBluetoothDevice, parameterName: nameof(nativeBluetoothDevice))
        )
        {
        }

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
            private readonly INativeFirmwareEraserCallbacksProxy _nativeEraserCallbacksProxy;

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
                
                _nativeEraserCallbacksProxy = eraserCallbacksProxy ?? throw new ArgumentNullException(nameof(eraserCallbacksProxy)); //composition-over-inheritance
            }

            public new void Dispose()
            {
                Dispose(disposing: true); //doesnt throw

                try
                {
                    base.Dispose();
                }
                catch
                {
                    //ignored
                }
                
                GC.SuppressFinalize(this);
            }

            private bool _alreadyDisposed;
            protected override void Dispose(bool disposing)
            {
                if (_alreadyDisposed)
                    return;

                if (!disposing)
                    return;
                
                TryCleanupInfrastructure();
                
                _alreadyDisposed = true;

                try
                {
                    base.Dispose(disposing: true);
                }
                catch
                {
                    //ignored
                }
            }
            
            private void TryCleanupInfrastructure()
            {
                try
                {
                    Disconnect();
                }
                catch
                {
                    // ignored
                }
            }

            public IFirmwareEraserEventEmittable FirmwareEraser //keep this to conform to the interface
            {
                get => _nativeEraserCallbacksProxy!.FirmwareEraser;
                set => _nativeEraserCallbacksProxy!.FirmwareEraser = value;
            }

            public new EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex)
            {
                if (_nativeEraserCallbacksProxy == null)
                    throw new InvalidOperationException("The native firmware eraser is not initialized");

                return TranslateEAndroidFirmwareEraserInitializationVerdict(base.BeginErasure(imageIndex));
            }

            public override void StateChangedAdvertisement(EAndroidFirmwareEraserState oldState, EAndroidFirmwareEraserState newState)
            {
                base.StateChangedAdvertisement(oldState, newState);

                StateChangedAdvertisement(
                    newState: TranslateEAndroidFirmwareEraserState(newState),
                    oldState: TranslateEAndroidFirmwareEraserState(oldState)
                );
            }
            
            //keep this override   it is needed to conform to the interface
            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState)
            {
                _nativeEraserCallbacksProxy?.StateChangedAdvertisement(newState: newState, oldState: oldState);
            }
            
            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case

                _nativeEraserCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle);
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage, int globalErrorCode)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage, globalErrorCode);

                FatalErrorOccurredAdvertisement(errorMessage, (EGlobalErrorCode) globalErrorCode);
            }
            
            public void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode)
            {
                _nativeEraserCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage, globalErrorCode);
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
                _nativeEraserCallbacksProxy?.LogMessageAdvertisement(message, category, level);
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
                
                if (state == EAndroidFirmwareEraserState.Failed)
                {
                    return EFirmwareErasureState.Failed;
                }
                
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown enum value");
            }

            static internal EFirmwareErasureInitializationVerdict TranslateEAndroidFirmwareEraserInitializationVerdict(EAndroidFirmwareEraserInitializationVerdict beginErasure)
            {
                if (beginErasure == EAndroidFirmwareEraserInitializationVerdict.Success)
                {
                    return EFirmwareErasureInitializationVerdict.Success;
                }
                
                if (beginErasure == EAndroidFirmwareEraserInitializationVerdict.FailedErrorUponCommencing)
                {
                    return EFirmwareErasureInitializationVerdict.FailedErrorUponCommencing;
                }
                
                if (beginErasure == EAndroidFirmwareEraserInitializationVerdict.FailedOtherErasureAlreadyInProgress)
                {
                    return EFirmwareErasureInitializationVerdict.FailedOtherErasureAlreadyInProgress;
                }
                
                throw new ArgumentOutOfRangeException(nameof(beginErasure), beginErasure, "Unknown enum value");
            }
        }
    }
}
