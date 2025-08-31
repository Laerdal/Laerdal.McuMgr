using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public EFileDownloaderVerdict BeginDownload(
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize = null,
            int? windowCapacity = null //not applicable currently   but nordic considers these for future use
        )
        {
            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

            remoteFilePath = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePath(remoteFilePath);

            var failsafeConnectionSettings = ConnectionSettingsHelpers.GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
                initialMtuSize: initialMtuSize,
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,
                uploadingNotDownloading: false
            );
            if (failsafeConnectionSettings != null)
            {
                initialMtuSize = failsafeConnectionSettings.Value.initialMtuSize;
                // windowCapacity = failsafeConnectionSettings.Value.windowCapacity;
                // memoryAlignment = failsafeConnectionSettings.Value.memoryAlignment;

                OnLogEmitted(new LogEmittedEventArgs(
                    level: ELogLevel.Warning,
                    message: $"[FD.BD.010] Host device '{hostDeviceModel} (made by {hostDeviceManufacturer})' is known to be problematic. Resorting to using failsafe settings (initialMtuSize={initialMtuSize})",
                    resource: "File",
                    category: "FileDownloader"
                ));
            }

            var verdict = _nativeFileDownloaderProxy.BeginDownload(
                remoteFilePath: remoteFilePath,
                initialMtuSize: initialMtuSize
            );

            return verdict;
        }
    }
}