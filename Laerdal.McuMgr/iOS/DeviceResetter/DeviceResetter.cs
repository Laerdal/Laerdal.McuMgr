// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Events;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.DeviceResetter
{
    /// <inheritdoc cref="IDeviceResetter"/>
    public partial class DeviceResetter : IDeviceResetter
    {
        private readonly IOSDeviceResetter _iosDeviceResetter;

        public DeviceResetter(CBPeripheral bleDevice)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));
            
            _iosDeviceResetter = new IOSDeviceResetter(
                listener: new IOSDeviceResetterListenerProxy(this),
                cbPeripheral: bleDevice
            );
        }

        public string LastFatalErrorMessage => _iosDeviceResetter?.LastFatalErrorMessage;

        public IDeviceResetter.EDeviceResetterState State => IOSDeviceResetterListenerProxy.TranslateEIOSDeviceResetterState(_iosDeviceResetter?.State ?? EIOSDeviceResetterState.None);

        public void BeginReset() => _iosDeviceResetter?.BeginReset();
        public void Disconnect() => _iosDeviceResetter?.Disconnect();

        // ReSharper disable once InconsistentNaming
        private sealed class IOSDeviceResetterListenerProxy : IOSListenerForDeviceResetter
        {
            private readonly DeviceResetter _resetter;

            internal IOSDeviceResetterListenerProxy(DeviceResetter resetter)
            {
                _resetter = resetter ?? throw new ArgumentNullException(nameof(resetter));
            }

            public override void LogMessageAdvertisement(string message, string category, string level) => _resetter.OnLogEmitted(new LogEmittedEventArgs(
                level: HelpersIOS.TranslateEIOSLogLevel(level),
                message: message,
                category: category,
                resource: "device-resetter"
            ));
            
            public override void FatalErrorOccurredAdvertisement(string errorMessage) => _resetter.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));

            public override void StateChangedAdvertisement(EIOSDeviceResetterState oldState, EIOSDeviceResetterState newState) => _resetter.OnStateChanged(new StateChangedEventArgs(
                newState: TranslateEIOSDeviceResetterState(newState),
                oldState: TranslateEIOSDeviceResetterState(oldState)
            ));

            // ReSharper disable once InconsistentNaming
            static public IDeviceResetter.EDeviceResetterState TranslateEIOSDeviceResetterState(EIOSDeviceResetterState state) => state switch
            {
                EIOSDeviceResetterState.None => IDeviceResetter.EDeviceResetterState.None,
                EIOSDeviceResetterState.Idle => IDeviceResetter.EDeviceResetterState.Idle,
                EIOSDeviceResetterState.Failed => IDeviceResetter.EDeviceResetterState.Failed,
                EIOSDeviceResetterState.Complete => IDeviceResetter.EDeviceResetterState.Complete,
                EIOSDeviceResetterState.Resetting => IDeviceResetter.EDeviceResetterState.Resetting,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }
    }
}