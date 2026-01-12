using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public async Task BeginUploadAsync( //this is meant to be used by the users   but our .UploadAsync() methods should *never* use this method
            byte[] data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            ELogLevel? minimumNativeLogLevel = null,
            int? initialMtuSize = null,
            int? pipelineDepth = null, //  ios
            int? byteAlignment = null, //  ios
            int? windowCapacity = null, // android
            int? memoryAlignment = null // android
        )
        {
            await EnsureExclusiveOperationTokenAsync().ConfigureAwait(false); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                BeginUploadCore(
                    data: data,

                    resourceId: resourceId,
                    remoteFilePath: remoteFilePath,
                    
                    hostDeviceModel: hostDeviceModel,
                    hostDeviceManufacturer: hostDeviceManufacturer,
                    
                    minimumNativeLogLevel: minimumNativeLogLevel,
                    
                    initialMtuSize: initialMtuSize,
                    pipelineDepth: pipelineDepth,
                    byteAlignment: byteAlignment,
                    windowCapacity: windowCapacity,
                    memoryAlignment: memoryAlignment
                );
            }
            finally
            {
                await ReleaseExclusiveOperationTokenAsync().ConfigureAwait(false);
            }
        }

        protected void BeginUploadCore( //this is meant to be used by the .UploadAsync() methods of our api surface
            byte[] data,
            string resourceId,
            string remoteFilePath,
            string hostDeviceModel,
            string hostDeviceManufacturer,
            ELogLevel? minimumNativeLogLevel,
            int? initialMtuSize,
            int? pipelineDepth, //  ios
            int? byteAlignment, //  ios
            int? windowCapacity, // android
            int? memoryAlignment

            // android
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
                    category: "FileUploader"
                ));
            }

            var verdict = NativeFileUploaderProxy.NativeBeginUpload(
                data: data,
                resourceId: resourceId,
                remoteFilePath: remoteFilePath,

                initialMtuSize: initialMtuSize,
                minimumNativeLogLevel: minimumNativeLogLevel,
                
                pipelineDepth: pipelineDepth,
                byteAlignment: byteAlignment,
                windowCapacity: windowCapacity,
                memoryAlignment: memoryAlignment
            );
            if (verdict != EFileUploaderVerdict.Success)
                throw verdict switch
                {
                    EFileUploaderVerdict.FailedInvalidData => new ArgumentException("The provided data were deemed invalid by the native layer (did you pass 'null' somehow - check logs for details)"),
                    EFileUploaderVerdict.FailedInvalidSettings => new ArgumentException("The provided connection settings were deemed invalid by the native layer (check logs for details)"),
                    EFileUploaderVerdict.FailedErrorUponCommencing => new FileUploadInternalErrorException(remoteFilePath: remoteFilePath, message: "An internal error occurred within the native layer upon commencing the upload operation"),
                    EFileUploaderVerdict.FailedOtherUploadAlreadyInProgress => new AnotherFileUploadIsAlreadyOngoingException(remoteFilePath: remoteFilePath),
                    _ => new ArgumentException($"An error occurred within the native layer [verdict={verdict}]"),
                };
            
            //00  we can get FailedOtherUploadAlreadyInProgress even here if there are two racing threads that have both called .BeginUpload() directly!
        }
    }
}
