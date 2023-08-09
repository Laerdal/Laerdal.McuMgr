// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using Laerdal.McuMgr.DeviceResetter.Contracts.Events;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        private readonly AndroidDeviceResetter _androidDeviceResetter;

        public DeviceResetter(BluetoothDevice bluetoothDevice, Context androidContext = null)
        {
            if (bluetoothDevice == null)
                throw new ArgumentNullException(nameof(bluetoothDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            _androidDeviceResetter = new AndroidDeviceResetterProxy(this, androidContext, bluetoothDevice);
        }

        public string LastFatalErrorMessage => _androidDeviceResetter?.LastFatalErrorMessage;

        public EDeviceResetterState State => AndroidDeviceResetterProxy.TranslateEAndroidDeviceResetterState(
            _androidDeviceResetter?.State ?? EAndroidDeviceResetterState.None
        );
        
        public void BeginReset() => _androidDeviceResetter.BeginReset();
        public void Disconnect() => _androidDeviceResetter.Disconnect();

        private sealed class AndroidDeviceResetterProxy : AndroidDeviceResetter
        {
            private readonly DeviceResetter _resetter;

            // ReSharper disable once UnusedMember.Local
            private AndroidDeviceResetterProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidDeviceResetterProxy(DeviceResetter resetter, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _resetter = resetter ?? throw new ArgumentNullException(nameof(resetter));
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage);
                
                _resetter.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
            }

            public override void StateChangedAdvertisement(EAndroidDeviceResetterState oldState, EAndroidDeviceResetterState newState)
            {
                base.StateChangedAdvertisement(oldState: oldState, currentState: newState);
                
                _resetter.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEAndroidDeviceResetterState(newState),
                    oldState: TranslateEAndroidDeviceResetterState(oldState)
                ));
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
