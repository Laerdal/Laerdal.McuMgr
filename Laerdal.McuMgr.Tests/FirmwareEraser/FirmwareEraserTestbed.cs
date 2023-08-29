using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Enums;
using Laerdal.McuMgr.FirmwareEraser.Contracts.Native;
using GenericNativeFirmwareEraserCallbacksProxy_ = Laerdal.McuMgr.FirmwareEraser.FirmwareEraser.GenericNativeFirmwareEraserCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareEraser
{
    public partial class FirmwareEraserTestbed
    {
        private class MockedNativeFirmwareEraserProxySpy : INativeFirmwareEraserProxy // template class for all spies
        {
            private readonly INativeFirmwareEraserCallbacksProxy _eraserCallbacksProxy;

            public bool DisconnectCalled { get; private set; }
            public bool BeginErasureCalled { get; private set; }

            public string LastFatalErrorMessage => "";

            public IFirmwareEraserEventEmittable FirmwareEraser //keep this to conform to the interface
            {
                get => _eraserCallbacksProxy!.FirmwareEraser;
                set => _eraserCallbacksProxy!.FirmwareEraser = value;
            }

            protected MockedNativeFirmwareEraserProxySpy(INativeFirmwareEraserCallbacksProxy eraserCallbacksProxy)
            {
                _eraserCallbacksProxy = eraserCallbacksProxy;
            }

            public virtual void BeginErasure(int imageIndex)
            {
                BeginErasureCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _eraserCallbacksProxy.LogMessageAdvertisement(message, category, level); //raises the actual event

            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState)
                => _eraserCallbacksProxy.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _eraserCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event

            public void FatalErrorOccurredAdvertisement(string errorMessage)
                => _eraserCallbacksProxy.FatalErrorOccurredAdvertisement(errorMessage); //raises the actual event
        }
    }
}