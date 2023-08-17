using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.DeviceResetter.Contracts;
using GenericNativeDeviceResetterCallbacksProxy_ = Laerdal.McuMgr.DeviceResetter.DeviceResetter.GenericNativeDeviceResetterCallbacksProxy;

namespace Laerdal.McuMgr.Tests.DeviceResetter
{
    public partial class DeviceResetterTestbed
    {
        private class MockedNativeDeviceResetterProxySpy : INativeDeviceResetterProxy // template class for other spies
        {
            private readonly INativeDeviceResetterCallbacksProxy _resetterCallbacksProxy;

            public bool DisconnectCalled { get; private set; }
            public bool BeginResetCalled { get; private set; }

            public object State { get; private set; }

            public string LastFatalErrorMessage => "";

            public IDeviceResetterEventEmitters DeviceResetter //keep this to conform to the interface
            {
                get => _resetterCallbacksProxy?.DeviceResetter;
                set
                {
                    if (_resetterCallbacksProxy == null)
                        return;

                    _resetterCallbacksProxy.DeviceResetter = value;
                }
            }

            protected MockedNativeDeviceResetterProxySpy(INativeDeviceResetterCallbacksProxy resetterCallbacksProxy)
            {
                _resetterCallbacksProxy = resetterCallbacksProxy;
            }

            public virtual void BeginReset()
            {
                BeginResetCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _resetterCallbacksProxy.LogMessageAdvertisement(message, category, level); //raises the actual event

            public void StateChangedAdvertisement(EDeviceResetterState oldState, EDeviceResetterState newState)
            {
                State = newState;
                _resetterCallbacksProxy.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event
            }

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => _resetterCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage); //raises the actual event
        }
    }
}