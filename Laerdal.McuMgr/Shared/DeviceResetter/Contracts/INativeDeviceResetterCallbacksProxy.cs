using Laerdal.McuMgr.Common;

namespace Laerdal.McuMgr.DeviceResetter.Contracts
{
    internal interface INativeDeviceResetterCallbacksProxy
    {
        public IDeviceResetterEventEmitters DeviceResetter { get; set; }

        public void LogMessageAdvertisement(string message, string category, ELogLevel level);
            
        public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState);

        public void FatalErrorOccurredAdvertisement(string errorMessage);
    }
}