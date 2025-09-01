using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public void BeginUpload( //this is meant to be used by the users   but our .UploadAsync() methods should *never* use this method
            byte[] data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize = null,
            int? pipelineDepth = null, //  ios
            int? byteAlignment = null, //  ios
            int? windowCapacity = null, // android
            int? memoryAlignment = null // android
        )
        {
            EnsureExclusiveOperationToken(); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                BeginUploadCore(
                    data: data,

                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,
                    
                    initialMtuSize: initialMtuSize,
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment
                );
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }
        }

        protected void BeginUploadCore( //this is meant to be used by the .DownloadAsync() methods of our api surface
            byte[] data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            int? initialMtuSize,
            int? pipelineDepth, //  ios
            int? byteAlignment, //  ios
            int? windowCapacity, // android
            int? memoryAlignment // android
        )
        {
            if (string.IsNullOrWhiteSpace(hostDeviceModel))
                throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

            if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));
            
            data = data ?? throw new ArgumentNullException(nameof(data));
            remoteFilePath = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePath(remoteFilePath);

            var failsafeConnectionSettings = ConnectionSettingsHelpers.GetFailSafeConnectionSettingsIfHostDeviceIsProblematic(
                hostDeviceModel: hostDeviceModel,
                hostDeviceManufacturer: hostDeviceManufacturer,

                initialMtuSize: initialMtuSize,
                uploadingNotDownloading: true,

                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,

                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );
            if (failsafeConnectionSettings != null)
            {
                initialMtuSize = failsafeConnectionSettings.Value.initialMtuSize;
                pipelineDepth = failsafeConnectionSettings.Value.pipelineDepth;
                byteAlignment = failsafeConnectionSettings.Value.byteAlignment;
                windowCapacity = failsafeConnectionSettings.Value.windowCapacity;
                memoryAlignment = failsafeConnectionSettings.Value.memoryAlignment;
                
                OnLogEmitted(new LogEmittedEventArgs(
                    level: ELogLevel.Warning,
                    message: $"[FU.BU.010] Host device '{hostDeviceModel} (made by {hostDeviceManufacturer})' is known to be problematic. Resorting to using failsafe settings " +
                             $"(pipelineDepth={pipelineDepth ?.ToString() ?? "null"}, byteAlignment={byteAlignment?.ToString() ?? "null"}, initialMtuSize={initialMtuSize?.ToString() ?? "null"}, windowCapacity={windowCapacity?.ToString() ?? "null"}, memoryAlignment={memoryAlignment?.ToString() ?? "null"})",
                    resource: resourceId,
                    category: "FileDownloader"
                ));
            }

            var verdict = NativeFileUploaderProxy.NativeBeginUpload(
                data: data,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,

                initialMtuSize: initialMtuSize,
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );
            if (verdict != EFileUploaderVerdict.Success)
                throw verdict == EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress //00
                    ? new InvalidOperationException("Another upload operation is already in progress")
                    : new ArgumentException(verdict.ToString());
            
            //00  we can get FailedOtherUploadAlreadyInProgress even here if there are two racing threads that have both called .BeginUpload() directly!
        }
    }
}
