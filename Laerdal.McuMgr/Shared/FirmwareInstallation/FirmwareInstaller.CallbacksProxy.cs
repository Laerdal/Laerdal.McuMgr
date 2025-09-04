using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        //this sort of approach proved to be necessary for our testsuite to be able to effectively mock away the INativeFirmwareInstallerProxy
        internal class GenericNativeFirmwareInstallerCallbacksProxy : INativeFirmwareInstallerCallbacksProxy
        {
            public IFirmwareInstallerEventEmittable FirmwareInstaller { get; set; }

            public void CancelledAdvertisement()
                => FirmwareInstaller?.OnCancelled(new CancelledEventArgs());

            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => FirmwareInstaller?.OnLogEmitted(new LogEmittedEventArgs(
                    level: level,
                    message: message,
                    category: category,
                    resource: resource
                ));

            public void StateChangedAdvertisement(EFirmwareInstallationState oldState, EFirmwareInstallationState newState)
                => FirmwareInstaller?.OnStateChanged(new StateChangedEventArgs(
                    newState: newState,
                    oldState: oldState
                ));

            // public void IdenticalFirmwareCachedOnTargetDeviceDetectedAdvertisement(...) //should not be implemented natively   this event is derived from onstatechanged and is not a native event!

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => FirmwareInstaller?.OnBusyStateChanged(new BusyStateChangedEventArgs(busyNotIdle));

            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, EGlobalErrorCode globalErrorCode)
                => FirmwareInstaller?.OnFatalErrorOccurred(new FatalErrorOccurredEventArgs(state, fatalErrorType, errorMessage, globalErrorCode));

            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => FirmwareInstaller?.OnFirmwareUploadProgressPercentageAndDataThroughputChanged(new FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs(
                    progressPercentage: progressPercentage,
                    currentThroughputInKBps: currentThroughputInKBps,
                    totalAverageThroughputInKBps: totalAverageThroughputInKBps
                ));
        }
    }
}