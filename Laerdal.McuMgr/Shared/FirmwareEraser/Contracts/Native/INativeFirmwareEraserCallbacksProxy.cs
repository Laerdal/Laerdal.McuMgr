using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts.Native
{
    internal interface INativeFirmwareEraserCallbacksProxy
    {
        IFirmwareEraserEventEmittable FirmwareEraser { get; set; }
            
        void LogMessageAdvertisement(string message, string category, ELogLevel level);
        void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(string errorMessage);
    }
}