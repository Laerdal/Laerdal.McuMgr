using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetting.Contracts.Native
{
    internal interface INativeDeviceResetterCallbacksProxy
    {
        public IDeviceResetterEventEmittable DeviceResetter { get; set; }

        public void LogMessageAdvertisement(string message, string category, ELogLevel level);

        public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState);

        public void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode);
    }
}