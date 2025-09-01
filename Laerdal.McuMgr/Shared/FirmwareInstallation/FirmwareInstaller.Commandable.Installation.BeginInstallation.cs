using System;
using System.Linq;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    public partial class FirmwareInstaller
    {
        public EFirmwareInstallationVerdict BeginInstallation(
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
            EnsureExclusiveOperationToken();
            
            try
            {
                var verdict = BeginInstallationCore(
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

                return verdict;
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }
        }
        
        protected EFirmwareInstallationVerdict BeginInstallationCore(
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

            _nativeFirmwareInstallerProxy.Nickname = "Firmware Installation"; //todo  get this from a parameter 
            var verdict = _nativeFirmwareInstallerProxy.BeginInstallation(
                mode: mode,
                data: data,

                eraseSettings: eraseSettings,
                estimatedSwapTimeInMilliseconds: estimatedSwapTimeInMilliseconds,
                
                initialMtuSize: initialMtuSize,

                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                
                windowCapacity: windowCapacity, //  android
                memoryAlignment: memoryAlignment // android
            );

            return verdict;
        }
    }
}
