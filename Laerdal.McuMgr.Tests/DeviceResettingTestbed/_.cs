using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.DeviceResetting.Contracts;
using Laerdal.McuMgr.DeviceResetting.Contracts.Enums;
using Laerdal.McuMgr.DeviceResetting.Contracts.Native;

namespace Laerdal.McuMgr.Tests.DeviceResettingTestbed
{
    public partial class DeviceResetterTestbed
    {
        private class MockedNativeDeviceResetterProxySpy : INativeDeviceResetterProxy // template class for other spies
        {
            private readonly INativeDeviceResetterCallbacksProxy _resetterCallbacksProxy;

            public bool DisconnectCalled { get; private set; }
            public bool BeginResetCalled { get; private set; }

            public EDeviceResetterState State { get; private set; }

            public string LastFatalErrorMessage => "";

            public IDeviceResetterEventEmittable DeviceResetter //keep this to conform to the interface
            {
                get => _resetterCallbacksProxy!.DeviceResetter;
                set => _resetterCallbacksProxy!.DeviceResetter = value;
            }

            protected MockedNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy)
            {
                _resetterCallbacksProxy = resetterCallbacksProxy;
            }

            public virtual EDeviceResetterInitializationVerdict BeginReset()
            {
                BeginResetCalled = true;
                
                return EDeviceResetterInitializationVerdict.Success;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _resetterCallbacksProxy?.LogMessageAdvertisement(message, category, level); //raises the actual event

            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
            {
                State = newState;
                _resetterCallbacksProxy?.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event
            }

            public void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode)
                => _resetterCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage, globalErrorCode); //raises the actual event

            public void Dispose()
            {
                // dud
            }
        }
    }
}