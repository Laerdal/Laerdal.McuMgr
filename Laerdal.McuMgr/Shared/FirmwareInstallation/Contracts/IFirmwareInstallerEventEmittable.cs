using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts
{
    public interface IFirmwareInstallerEventEmittable : ILogEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnOverallProgressPercentageChanged(OverallProgressPercentageChangedEventArgs ea);
        void OnIdenticalFirmwareCachedOnTargetDeviceDetected(IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea);
        void OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}