// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using CoreBluetooth;
using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Events;
using McuMgrBindingsiOS;

namespace Laerdal.McuMgr.FirmwareEraser
{
    /// <inheritdoc cref="IFirmwareEraser"/>
    public partial class FirmwareEraser : IFirmwareEraser
    {
        private readonly IOSFirmwareEraser _iosFirmwareEraser;

        public FirmwareEraser(CBPeripheral bleDevice)
        {
            if (bleDevice == null)
                throw new ArgumentNullException(nameof(bleDevice));
            
            _iosFirmwareEraser = new IOSFirmwareEraser(
                listener: new IOSFirmwareEraserListenerProxy(this),
                cbPeripheral: bleDevice
            );
        }

        public string LastFatalErrorMessage => _iosFirmwareEraser?.LastFatalErrorMessage;

        public void Disconnect() => _iosFirmwareEraser.Disconnect();
        public void BeginErasure(int imageIndex = 1) => _iosFirmwareEraser.BeginErasure(imageIndex);

        // ReSharper disable once InconsistentNaming
        private sealed class IOSFirmwareEraserListenerProxy : IOSListenerForFirmwareEraser
        {
            private readonly FirmwareEraser _eraser;

            internal IOSFirmwareEraserListenerProxy(FirmwareEraser eraser)
            {
                _eraser = eraser ?? throw new ArgumentNullException(nameof(eraser));
            }

            public override void FatalErrorOccurredAdvertisement(string errorMessage) => _eraser.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(errorMessage));
            public override void BusyStateChangedAdvertisement(bool busyNotIdle) => _eraser.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public override void LogMessageAdvertisement(string message, string category, string level) => _eraser.OnLogEmitted(new LogEmittedEventArgs(
                level: HelpersIOS.TranslateEIOSLogLevel(level),
                message: message,
                category: category,
                resource: "firmware-eraser"
            ));
            
            public override void StateChangedAdvertisement(EIOSFirmwareEraserState oldState, EIOSFirmwareEraserState newState) => _eraser.OnStateChanged(new StateChangedEventArgs(
                newState: TranslateEIOSFirmwareEraserState(newState),
                oldState: TranslateEIOSFirmwareEraserState(oldState)
            ));

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