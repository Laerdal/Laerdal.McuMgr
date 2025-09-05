using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Enums;
using Laerdal.McuMgr.FirmwareErasure.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareErasureTestbed
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

            public virtual EFirmwareErasureInitializationVerdict BeginErasure(int imageIndex)
            {
                BeginErasureCalled = true;
                
                return EFirmwareErasureInitializationVerdict.Success;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void LogMessageAdvertisement(string message, string category, ELogLevel level)
                => _eraserCallbacksProxy?.LogMessageAdvertisement(message, category, level); //raises the actual event

            public void StateChangedAdvertisement(EFirmwareErasureState oldState, EFirmwareErasureState newState)
                => _eraserCallbacksProxy?.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _eraserCallbacksProxy?.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event

            public void FatalErrorOccurredAdvertisement(string errorMessage, EGlobalErrorCode globalErrorCode)
                => _eraserCallbacksProxy?.FatalErrorOccurredAdvertisement(errorMessage, globalErrorCode); //raises the actual event

            public void Dispose()
            {
                // nothing to do
            }
        }
    }
}