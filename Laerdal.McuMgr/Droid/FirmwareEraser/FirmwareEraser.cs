// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Runtime;
using Laerdal.Java.McuMgr.Wrapper.Android;
using Laerdal.McuMgr.FirmwareEraser.Events;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        private readonly AndroidFirmwareEraser _androidFirmwareEraser;

        public FirmwareEraser(BluetoothDevice bleDevice, Context androidContext = null)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));

            androidContext ??= Application.Context;
            if (androidContext == null)
                throw new InvalidOperationException("Failed to retrieve the Android Context in which this call takes place - this is weird");

            _androidFirmwareEraser = new AndroidFirmwareEraserProxy(this, androidContext, bleDevice);
        }

        public string LastFatalErrorMessage => _androidFirmwareEraser?.LastFatalErrorMessage;

        public void Disconnect() => _androidFirmwareEraser.Disconnect();
        public void BeginErasure(int imageIndex = 1) => _androidFirmwareEraser.BeginErasure(imageIndex);

        private sealed class AndroidFirmwareEraserProxy : AndroidFirmwareEraser
        {
            private readonly FirmwareEraser _eraser;

            // ReSharper disable once UnusedMember.Local
            private AndroidFirmwareEraserProxy(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            internal AndroidFirmwareEraserProxy(FirmwareEraser eraser, Context context, BluetoothDevice bluetoothDevice) : base(context, bluetoothDevice)
            {
                _eraser = eraser ?? throw new ArgumentNullException(nameof(eraser));
            }

            public override void StateChangedAdvertisement(EAndroidFirmwareEraserState oldState, EAndroidFirmwareEraserState newState)
            {
                base.StateChangedAdvertisement(oldState, newState);
                
                _eraser.OnStateChanged(new StateChangedEventArgs(
                    newState: TranslateEAndroidFirmwareEraserState(newState),
                    oldState: TranslateEAndroidFirmwareEraserState(oldState)
                ));
            }
            
            public override void BusyStateChangedAdvertisement(bool busyNotIdle)
            {
                base.BusyStateChangedAdvertisement(busyNotIdle); //just in case

                _eraser.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage)
            {
                base.FatalErrorOccurredAdvertisement(errorMessage);
                
                _eraser.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
            }

            static private IFirmwareEraser.EFirmwareErasureState TranslateEAndroidFirmwareEraserState(EAndroidFirmwareEraserState state)
            {
                if (state == EAndroidFirmwareEraserState.None)
                {
                    return IFirmwareEraser.EFirmwareErasureState.None;
                }
                
                if (state == EAndroidFirmwareEraserState.Idle)
                {
                    return IFirmwareEraser.EFirmwareErasureState.Idle;
                }

                if (state == EAndroidFirmwareEraserState.Erasing)
                {
                    return IFirmwareEraser.EFirmwareErasureState.Erasing;
                }

                if (state == EAndroidFirmwareEraserState.Complete)
                {
                    return IFirmwareEraser.EFirmwareErasureState.Complete;
                }
                
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
