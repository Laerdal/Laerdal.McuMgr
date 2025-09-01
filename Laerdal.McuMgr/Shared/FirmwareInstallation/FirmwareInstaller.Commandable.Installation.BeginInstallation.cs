using System;
using System.Linq;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public void BeginInstallation(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? initialMtuSize = null,
            int? windowCapacity = null, //   android only    not applicable for ios
            int? memoryAlignment = null, //  android only    not applicable for ios
            int? pipelineDepth = null, //    ios only        not applicable for android
            int? byteAlignment = null //     ios only        not applicable for android
        )
        {
            EnsureExclusiveOperationToken(); //keep this outside of the try-finally block!
            
            try
            {
                BeginInstallationCore(
                    mode: mode,
                    data: data,
                    
                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,
                    
                    eraseSettings: eraseSettings,
                    estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds,
                    
                    initialMtuSize: initialMtuSize,

                    pipelineDepth: pipelineDepth, //ios
                    byteAlignment: byteAlignment, //ios
                    
                    windowCapacity: windowCapacity, //android
                    memoryAlignment: memoryAlignment //android
                );
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }
        }
        
        protected void BeginInstallationCore(
            byte[] data,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            EFirmwareInstallationMode mode,
            bool? eraseSettings,
            int? estimatedSwapTimeInMilliseconds,
            int? initialMtuSize,
            int? windowCapacity, //   android only    not applicable for ios
            int? memoryAlignment, //  android only    not applicable for ios
            int? pipelineDepth, //    ios only        not applicable for android
            int? byteAlignment //     ios only        not applicable for android
        )
        {
            if (data == null || !data.Any())
                throw new ArgumentException("The data byte-array parameter is null or empty", nameof(data));

            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

            var failsafeConnectionSettings = ConnectionSettingsHelpers.GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
                uploadingNotDownloading: true,

                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,

                initialMtuSize: initialMtuSize,

                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );
            if (failsafeConnectionSettings != null)
            {
                pipelineDepth = failsafeConnectionSettings.Value.pipelineDepth;
                byteAlignment = failsafeConnectionSettings.Value.byteAlignment;
                initialMtuSize = failsafeConnectionSettings.Value.initialMtuSize;
                windowCapacity = failsafeConnectionSettings.Value.windowCapacity;
                memoryAlignment = failsafeConnectionSettings.Value.memoryAlignment;

                OnLogEmitted(new LogEmittedEventArgs(
                    level: ELogLevel.Warning,
                    message: $"[FI.BI.010] Host device '{hostDeviceModel} (made by {hostDeviceManufacturer})' is known to be problematic. Resorting to using failsafe settings " +
                             $"(pipelineDepth={pipelineDepth?.ToString() ?? "null"}, byteAlignment={byteAlignment?.ToString() ?? "null"}, initialMtuSize={initialMtuSize?.ToString() ?? "null"}, windowCapacity={windowCapacity?.ToString() ?? "null"}, memoryAlignment={memoryAlignment?.ToString() ?? "null"})",
                    resource: "File",
                    category: "FileDownloader"
                ));
            }

            NativeFirmwareInstallerProxy.Nickname = "Firmware Installation"; //todo  get this from a parameter 

            var verdict = NativeFirmwareInstallerProxy.NativeBeginInstallation( //throws an exception if something is wrong
                data: data,

                mode: mode,
                eraseSettings: eraseSettings,
                estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds,

                initialMtuSize: initialMtuSize, //    ios + android
                
                pipelineDepth: pipelineDepth, //      ios
                memoryAlignment: memoryAlignment, //  ios
                
                byteAlignment: byteAlignment, //      android
                windowCapacity: windowCapacity //     android
            );
            if (verdict != EFirmwareInstallationVerdict.Success)
                throw verdict switch
                {
                    EFirmwareInstallationVerdict.FailedInvalidSettings => new ArgumentException("The provided connection settings were deemed invalid by the native layer (check logs for details)"),
                    EFirmwareInstallationVerdict.FailedGivenFirmwareUnhealthy => new ArgumentException("The provided firmware was deemed invalid by the native layer (check logs for details)"),
                    EFirmwareInstallationVerdict.FailedInstallationAlreadyInProgress => new InvalidOperationException("Another installation is already in progress"),
                    EFirmwareInstallationVerdict.FailedInstallationInitializationErroredOut => new FirmwareInstallationInternalErrorException("The native layer failed to initialize the installation"),
                    _ => new ArgumentException($"An error occurred within the native layer [verdict={verdict}]"),
                };
        }
    }
}
