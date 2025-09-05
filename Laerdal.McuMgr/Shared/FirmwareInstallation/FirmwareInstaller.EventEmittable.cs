using System;
using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Extensions;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        private event EventHandler<CancelledEventArgs> _cancelled;
        private event EventHandler<StateChangedEventArgs> _stateChanged;
        private event EventHandler<BusyStateChangedEventArgs> _busyStateChanged;
        private event EventHandler<FatalErrorOccurredEventArgs> _fatalErrorOccurred;
        private event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> _logEmitted;
        private event EventHandler<OverallProgressPercentageChangedEventArgs> _overallProgressPercentageChanged;
        private event EventHandler<IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs> _identicalFirmwareCachedOnTargetDeviceDetected;
        private event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> _firmwareUploadProgressPercentageAndDataThroughputChanged;

        public event EventHandler<FatalErrorOccurredEventArgs> FatalErrorOccurred
        {
            add
            {
                _fatalErrorOccurred -= value;
                _fatalErrorOccurred += value;
            }
            remove => _fatalErrorOccurred -= value;
        }

        public event ZeroCopyEventHelpers.ZeroCopyEventHandler<LogEmittedEventArgs> LogEmitted
        {
            add
            {
                _logEmitted -= value;
                _logEmitted += value;
            }
            remove => _logEmitted -= value;
        }

        public event EventHandler<CancelledEventArgs> Cancelled
        {
            add
            {
                _cancelled -= value;
                _cancelled += value;
            }
            remove => _cancelled -= value;
        }

        public event EventHandler<BusyStateChangedEventArgs> BusyStateChanged
        {
            add
            {
                _busyStateChanged -= value;
                _busyStateChanged += value;
            }
            remove => _busyStateChanged -= value;
        }

        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            add
            {
                _stateChanged -= value;
                _stateChanged += value;
            }
            remove => _stateChanged -= value;
        }

        public event EventHandler<IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs> IdenticalFirmwareCachedOnTargetDeviceDetected
        {
            add
            {
                _identicalFirmwareCachedOnTargetDeviceDetected -= value;
                _identicalFirmwareCachedOnTargetDeviceDetected += value;
            }
            remove => _identicalFirmwareCachedOnTargetDeviceDetected -= value;
        }

        public event EventHandler<FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs> FirmwareUploadProgressPercentageAndDataThroughputChanged
        {
            add
            {
                _firmwareUploadProgressPercentageAndDataThroughputChanged -= value;
                _firmwareUploadProgressPercentageAndDataThroughputChanged += value;
            }
            remove => _firmwareUploadProgressPercentageAndDataThroughputChanged -= value;
        }
        
        public event EventHandler<OverallProgressPercentageChangedEventArgs> OverallProgressPercentageChanged
        {
            add
            {
                _overallProgressPercentageChanged -= value;
                _overallProgressPercentageChanged += value;
            }
            remove => _overallProgressPercentageChanged -= value;
        }

        void ILogEmittable.OnLogEmitted(in LogEmittedEventArgs ea) => OnLogEmitted(in ea);
        void IFirmwareInstallerEventEmittable.OnCancelled(CancelledEventArgs ea) => OnCancelled(ea); //just to make the class unit-test friendly without making the methods public
        void IFirmwareInstallerEventEmittable.OnStateChanged(StateChangedEventArgs ea) => OnStateChanged(ea);
        void IFirmwareInstallerEventEmittable.OnBusyStateChanged(BusyStateChangedEventArgs ea) => OnBusyStateChanged(ea);
        void IFirmwareInstallerEventEmittable.OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea) => OnFatalErrorOccurred(ea);
        void IFirmwareInstallerEventEmittable.OnOverallProgressPercentageChanged(OverallProgressPercentageChangedEventArgs ea) => OnOverallProgressPercentageChanged(ea);
        void IFirmwareInstallerEventEmittable.OnIdenticalFirmwareCachedOnTargetDeviceDetected(IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea) => OnIdenticalFirmwareCachedOnTargetDeviceDetected(ea);
        void IFirmwareInstallerEventEmittable.OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea) => OnFirmwareUploadProgressPercentageAndDataThroughputChanged(ea);

        private void OnCancelled(CancelledEventArgs ea) => _cancelled?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea); //                        we suppress exceptions here because if we allow them to bubble towards the native code then the
        private void OnLogEmitted(in LogEmittedEventArgs ea) => _logEmitted?.InvokeAndIgnoreExceptions(this, in ea); // in the special case of log-emitted we prefer the .invoke() flavour for the sake of performance 
        private void OnBusyStateChanged(BusyStateChangedEventArgs ea) => _busyStateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea); //   native code can crash which can potentially cause very nasty problems in the firmware installation
        private void OnIdenticalFirmwareCachedOnTargetDeviceDetected(IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea) => _identicalFirmwareCachedOnTargetDeviceDetected?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);

        private void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea)
        {
            OnLogEmitted(new LogEmittedEventArgs(
                level: ELogLevel.Error,
                message: $"[{nameof(ea.State)}='{ea.State}'] [{nameof(ea.GlobalErrorCode)}='{ea.GlobalErrorCode}'] [{nameof(ea.FatalErrorType)}='{ea.FatalErrorType}'] {ea.ErrorMessage}",
                resource: "",
                category: "firmware-installer"
            ));

            OnFatalErrorOccurred_(ea);
            return;

            void OnFatalErrorOccurred_(FatalErrorOccurredEventArgs ea_)
            {
                _fatalErrorOccurred?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea_);
            }
        }
        
        private int _fileUploadProgressEventsCount;
        private void OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea)
        {
            _fileUploadProgressEventsCount++; //order
            _firmwareUploadProgressPercentageAndDataThroughputChanged?.InvokeAndIgnoreExceptions(this, ea); //order   typically there is only one subscriber to this event so we can use the simple .invoke() flavour here
            
            if (FirmwareInstallationOverallProgressPercentage < UploadingPhaseProgressMilestonePercentFinish) //10  hack
            {
                FirmwareInstallationOverallProgressPercentage = UploadingPhaseProgressMilestonePercentStart + (int)(ea.ProgressPercentage * 0.4f); //10% to 50%
            }

            //10  we noticed that there is a small race condition between state changes and the progress% updates   we first get a state change to 'resetting' (70%)
            //    and then a file-upload progress% update to 100%   we would like to fix this inside the native firmware installer library but it is quite hard to do so
        }

        private int _firmwareInstallationOverallProgressPercentage;
        private int FirmwareInstallationOverallProgressPercentage
        {
            get => _firmwareInstallationOverallProgressPercentage;
            set
            {
                if (value >= 1 && _firmwareInstallationOverallProgressPercentage >= value) //fend off out-of-order progress updates except for the initial 1% value
                    return;

                _firmwareInstallationOverallProgressPercentage = value;
                OnOverallProgressPercentageChanged(new OverallProgressPercentageChangedEventArgs(value));
            }
        }

        private void OnOverallProgressPercentageChanged(OverallProgressPercentageChangedEventArgs ea)
        {
            _overallProgressPercentageChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea);
        }

        private void OnStateChanged(StateChangedEventArgs ea)
        {
            try
            {
                FirmwareInstallationOverallProgressPercentage = GetProgressMilestonePercentageForState(ea.NewState) ?? FirmwareInstallationOverallProgressPercentage;

                TacklePossibleStateSideEffects_();
            }
            finally
            {
                _stateChanged?.InvokeAllEventHandlersAndIgnoreExceptions(this, ea); //00 must be dead last
            }

            return;

            void TacklePossibleStateSideEffects_()
            {
                switch (ea)
                {
                    case { NewState: EFirmwareInstallationState.Idle }:
                        _fileUploadProgressEventsCount = 0; //its vital to reset the counter here to account for retries
                        break;

                    case { NewState: EFirmwareInstallationState.Testing } when _fileUploadProgressEventsCount <= 1: //works both on ios and android
                        OnIdenticalFirmwareCachedOnTargetDeviceDetected(new(ECachedFirmwareType.CachedButInactive));
                        break;

                    case { NewState: EFirmwareInstallationState.Complete } when _fileUploadProgressEventsCount <= 1: //works both on ios and android
                        OnIdenticalFirmwareCachedOnTargetDeviceDetected(new(ECachedFirmwareType.CachedAndActive));
                        break;
                }
            }
            
            //00  if we raise the state-changed event before the switch statement then the calling environment will unwire the event handlers of
            //    the identical-firmware-cached-on-target-device-detected event before it gets fired and the event will be ignored altogether
        }
        
        static private readonly int UploadingPhaseProgressMilestonePercentStart = GetProgressMilestonePercentageForState(EFirmwareInstallationState.Uploading)!.Value;
        static private readonly int UploadingPhaseProgressMilestonePercentFinish = GetProgressMilestonePercentageForState(EFirmwareInstallationState.Testing)!.Value;
        
        static internal int? GetProgressMilestonePercentageForState(EFirmwareInstallationState state) => state switch //@formatter:off
        {
            EFirmwareInstallationState.None         => 0,
            EFirmwareInstallationState.Idle         => 1,
            EFirmwareInstallationState.Validating   => 2,
            EFirmwareInstallationState.Uploading    => 10, //00
            EFirmwareInstallationState.Testing      => 50,
            EFirmwareInstallationState.Resetting    => 70,
            EFirmwareInstallationState.Confirming   => 80,
            EFirmwareInstallationState.Complete     => 100,
            _ => null // .error .paused .cancelled .cancelling    we shouldnt throw an exception here   @formatter:on
        
            //00   note that the progress% is further updated from 10% to 50% by the upload process via the event FirmwareUploadProgressPercentageAndDataThroughputChanged
        };
    }
}