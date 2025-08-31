using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;

namespace Laerdal.McuMgr.Tests.FirmwareInstallationTestbed
{
    public partial class FirmwareInstallerTestbed
    {
        private class MockedNativeFirmwareInstallerProxySpy : INativeFirmwareInstallerProxy //template class for all spies
        {
            private readonly INativeFirmwareInstallerCallbacksProxy _firmwareInstallerCallbacksProxy;

            public string Nickname { get; set; }

            public bool CancelCalled { get; private set; }
            public bool DisconnectCalled { get; private set; }
            public bool BeginInstallationCalled { get; private set; }

            public string LastFatalErrorMessage => "";

            public IFirmwareInstallerEventEmittable FirmwareInstaller //keep this to conform to the interface
            {
                get => _firmwareInstallerCallbacksProxy!.FirmwareInstaller;
                set => _firmwareInstallerCallbacksProxy!.FirmwareInstaller = value;
            }

            protected MockedNativeFirmwareInstallerProxySpy(INativeFirmwareInstallerCallbacksProxy firmwareInstallerCallbacksProxy)
            {
                _firmwareInstallerCallbacksProxy = firmwareInstallerCallbacksProxy;
            }

            public virtual EFirmwareInstallationVerdict BeginInstallation(
                byte[] data,
                EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
                bool? eraseSettings = null,
                int? estimatedSwapTimeInMilliseconds = null,
                int? initialMtuSize = null,
                int? windowCapacity = null,
                int? memoryAlignment = null,
                int? pipelineDepth = null,
                int? byteAlignment = null
            )
            {
                BeginInstallationCalled = true;

                return EFirmwareInstallationVerdict.Success;
            }

            public virtual void Cancel()
            {
                CancelCalled = true;
            }

            public virtual void Disconnect()
            {
                DisconnectCalled = true;
            }

            public void CancelledAdvertisement() 
                => _firmwareInstallerCallbacksProxy.CancelledAdvertisement(); //raises the actual event
            
            public void LogMessageAdvertisement(string message, string category, ELogLevel level, string resource)
                => _firmwareInstallerCallbacksProxy.LogMessageAdvertisement(message, category, level, resource); //raises the actual event

            public void StateChangedAdvertisement(EFirmwareInstallationState oldState, EFirmwareInstallationState newState)
                => _firmwareInstallerCallbacksProxy.StateChangedAdvertisement(newState: newState, oldState: oldState); //raises the actual event

            public void BusyStateChangedAdvertisement(bool busyNotIdle)
                => _firmwareInstallerCallbacksProxy.BusyStateChangedAdvertisement(busyNotIdle); //raises the actual event
            
            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage, EGlobalErrorCode globalErrorCode)
                => _firmwareInstallerCallbacksProxy.FatalErrorOccurredAdvertisement(state, fatalErrorType, errorMessage, globalErrorCode); //raises the actual event
            
            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float currentThroughputInKBps, float totalAverageThroughputInKBps)
                => _firmwareInstallerCallbacksProxy.FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, currentThroughputInKBps, totalAverageThroughputInKBps); //raises the actual event

            public void TryCleanupResourcesOfLastInstallation()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}