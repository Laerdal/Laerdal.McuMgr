using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Events;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts
{
    internal interface IFirmwareInstallerEventEmittable
    {
        void OnCancelled(CancelledEventArgs ea);
        void OnLogEmitted(LogEmittedEventArgs ea);
        void OnStateChanged(StateChangedEventArgs ea);
        void OnBusyStateChanged(BusyStateChangedEventArgs ea);
        void OnFatalErrorOccurred(FatalErrorOccurredEventArgs ea);
        void OnFirmwareUploadProgressPercentageAndDataThroughputChanged(FirmwareUploadProgressPercentageAndDataThroughputChangedEventArgs ea);
    }
}