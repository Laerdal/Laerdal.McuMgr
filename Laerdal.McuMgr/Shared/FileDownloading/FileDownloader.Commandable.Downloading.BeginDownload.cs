using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public void BeginDownload(string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize = null,
            int? windowCapacity = null //not applicable currently   but nordic considers these for future use
        )
        {
            EnsureExclusiveOperationToken(); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                BeginDownloadCore(
                    remoteFilePath: remoteFilePath,
                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,

                    initialMtuSize: initialMtuSize,
                    windowCapacity: windowCapacity
                );
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }
        }
        
        protected void BeginDownloadCore( //meant to be used directly by the .DownloadAsync() methods of our api surface
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize,
            int? windowCapacity //not applicable currently   but nordic considers these for future use
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

            var verdict = NativeFileDownloaderProxy.NativeBeginDownload(
                remoteFilePath: remoteFilePath,
                initialMtuSize: initialMtuSize
            );
            if (verdict != EFileDownloaderVerdict.Success)
                throw verdict switch
                {
                    EFileDownloaderVerdict.FailedInvalidSettings => new ArgumentException("The provided connection settings were deemed invalid by the native layer (check logs for details)"),
                    EFileDownloaderVerdict.FailedErrorUponCommencing => new DownloadInternalErrorException("An internal error occurred within the native layer upon commencing the download operation"),
                    EFileDownloaderVerdict.FailedDownloadAlreadyInProgress => new InvalidOperationException("Another download operation is already in progress"),
                    _ => new ArgumentException($"An error occurred within the native layer [verdict={verdict}]"),
                };
        }
    }
}