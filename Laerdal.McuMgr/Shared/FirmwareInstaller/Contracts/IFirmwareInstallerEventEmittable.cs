using Laerdal.McuMgr.Common.Contracts;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts
{
    internal interface IFirmwareInstallerEventEmittable : ILogEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnIdenticalFirmwareCachedOnTargetDeviceDetected(IdenticalFirmwareCachedOnTargetDeviceDetectedEventArgs ea);
        void OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}