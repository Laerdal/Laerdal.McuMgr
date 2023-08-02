// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.DeviceResetter.Events;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        private readonly AndroidDeviceResetter _androidDeviceResetter;

        public DeviceResetter(BluetoothDevice bleDevice, Context androidContext = null)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            _androidDeviceResetter = new AndroidDeviceResetterProxy(this, androidContext, bleDevice);
        }

        public string LastFatalErrorMessage => _androidDeviceResetter?.LastFatalErrorMessage;

        public IDeviceResetter.EDeviceResetterState State => AndroidDeviceResetterProxy.TranslateEAndroidDeviceResetterState(
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

            static public IDeviceResetter.EDeviceResetterState TranslateEAndroidDeviceResetterState(EAndroidDeviceResetterState state)
            {
                if (state == EAndroidDeviceResetterState.None)
                {
                    return IDeviceResetter.EDeviceResetterState.None;
                }
                
                if (state == EAndroidDeviceResetterState.Idle)
                {
                    return IDeviceResetter.EDeviceResetterState.Idle;
                }

                if (state == EAndroidDeviceResetterState.Resetting)
                {
                    return IDeviceResetter.EDeviceResetterState.Resetting;
                }

                if (state == EAndroidDeviceResetterState.Complete)
                {
                    return IDeviceResetter.EDeviceResetterState.Complete;
                }
                
                if (state == EAndroidDeviceResetterState.Failed)
                {
                    return IDeviceResetter.EDeviceResetterState.Failed;
                }
                
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
