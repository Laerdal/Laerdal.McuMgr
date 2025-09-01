using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Native
{
    public interface INativeFirmwareInstallerCallbacksProxy
    {
        public IFirmwareInstallerEventEmittable FirmwareInstaller { get; set; }
        
        void CancelledAdvertisement();
        void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource);
        void StateChangedAdvertisement(EFirmwareInstallationState oldState, EFirmwareInstallationState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, EGlobalErrorCode globalErrorCode);
        void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps);
    }
}
