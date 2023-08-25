using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts.Enums;

namespace Laerdal.McuMgr.DeviceResetter.Contracts.Native
{
    internal interface INativeDeviceResetterCallbacksProxy
    {
        public IDeviceResetterEventEmittable DeviceResetter { get; set; }

        public void LogMessageAdvertisement(string message, string category, ELogLevel level);

        public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState);

        public void FatalErrorOccurredAdvertisement(string errorMessage);
    }
}