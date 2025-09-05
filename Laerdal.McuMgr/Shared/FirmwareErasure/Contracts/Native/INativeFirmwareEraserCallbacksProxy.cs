using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareErasure.Contracts.Native
{
    internal interface INativeFirmwareEraserCallbacksProxy
    {
        IFirmwareEraserEventEmittable FirmwareEraser { get; set; }
            
        void LogMessageAdvertisement(string message, string category, ELogLevel level);
        void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode);
    }
}