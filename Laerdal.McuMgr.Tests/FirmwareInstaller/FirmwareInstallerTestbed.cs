using Laerdal.McuMgr.Common;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Native;
using GenericNativeFirmwareInstallerCallbacksProxy_ = Laerdal.McuMgr.FirmwareInstaller.FirmwareInstaller.GenericNativeFirmwareInstallerCallbacksProxy;

namespace Laerdal.McuMgr.Tests.FirmwareInstaller
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
                get => _firmwareInstallerCallbacksProxy?.FirmwareInstaller;
                set
                {
                    if (_firmwareInstallerCallbacksProxy == null)
                        return;

                    _firmwareInstallerCallbacksProxy.FirmwareInstaller = value;
                }
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
            
            public void FatalErrorOccurredAdvertisement(EFirmwareInstallationState state, EFirmwareInstallerFatalErrorType fatalErrorType, string errorMessage)
                => _firmwareInstallerCallbacksProxy.FatalErrorOccurredAdvertisement(state, fatalErrorType, errorMessage); //raises the actual event
            
            public void FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(int progressPercentage, float averageThroughput)
                => _firmwareInstallerCallbacksProxy.FirmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput); //raises the actual event
        }
    }
}