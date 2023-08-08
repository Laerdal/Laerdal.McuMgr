using Laerdal.McuMgr.Common;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface INativeFirmwareEraserCallbacksProxy
    {
        IFirmwareEraserEventEmitters FirmwareEraser { get; set; }
            
        void LogMessageAdvertisement(string message, string category, ELogLevel level);
        void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(string errorMessage);
    }
}