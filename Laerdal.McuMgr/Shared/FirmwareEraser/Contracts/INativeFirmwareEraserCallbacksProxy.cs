using Laerdal.McuMgr.Common;

namespace Laerdal.McuMgr.FirmwareEraser.Contracts
{
    internal interface INativeFirmwareEraserCallbacksProxy
    {
        FirmwareEraser GenericFirmwareEraser { get; set; }
            
        void LogMessageAdvertisement(string message, string category, ELogLevel level);
        void StateChangedAdvertisement(IFirmwareEraser.EFirmwareErasureState oldState, IFirmwareEraser.EFirmwareErasureState newState);
        void BusyStateChangedAdvertisement(bool busyNotIdle);
        void FatalErrorOccurredAdvertisement(string errorMessage);
    }
}