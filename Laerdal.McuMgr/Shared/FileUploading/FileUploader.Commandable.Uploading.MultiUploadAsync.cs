// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public async Task<IEnumerable<string>> UploadAsync<TData>( //@formatter:off
            IDictionary<string, (string ResourceId, TData Data)> remoteFilePathsAndTheirData,

            string hostDeviceModel,
            string hostDeviceManufacturer,
            
            int    sleepTimeBetweenUploadsInMs     = IFileUploaderCommandable.Defaults.SleepTimeBetweenUploadsInMs,
            int    sleepTimeBetweenRetriesInMs     = IFileUploaderCommandable.Defaults.SleepTimeBetweenRetriesInMs,
            int    gracefulCancellationTimeoutInMs = IFileUploaderCommandable.Defaults.GracefulCancellationTimeoutInMs,
            int    timeoutPerUploadInMs            = IFileUploaderCommandable.Defaults.TimeoutPerUploadInMs,
            int    maxTriesPerUpload               = IFileUploaderCommandable.Defaults.MaxTriesPerUpload,
            bool   moveToNextUploadInCaseOfError   = IFileUploaderCommandable.Defaults.MoveToNextUploadInCaseOfError,
            bool   autodisposeStreams              = IFileUploaderCommandable.Defaults.AutodisposeStreams,
            
            int?   initialMtuSize = null,
            int?   pipelineDepth = null,
            int?   byteAlignment = null,
            int?   windowCapacity = null,
            int?   memoryAlignment = null
        ) where TData : notnull //@formatter:on
        {
            EnsureExclusiveOperationToken(); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                return await MultiUploadCoreAsync_();
            }
            finally
            {
                ReleaseExclusiveOperationToken();
            }

            async Task<IEnumerable<string>> MultiUploadCoreAsync_()
            {
                if (string.IsNullOrWhiteSpace(hostDeviceModel))
                    throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

                if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                    throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

                if (sleepTimeBetweenUploadsInMs < 0)
                    throw new ArgumentOutOfRangeException(nameof(sleepTimeBetweenUploadsInMs), sleepTimeBetweenUploadsInMs, "Must be greater than or equal to zero");

                var sanitizedRemoteFilePathsAndTheirData = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePathsWithData(remoteFilePathsAndTheirData);

                var lastIndex = sanitizedRemoteFilePathsAndTheirData.Count - 1;
                var filesThatFailedToBeUploaded = (List<string>) null;
                foreach (var ((remoteFilePath, (resourceId, data)), i) in sanitizedRemoteFilePathsAndTheirData.Select((x, i) => (x, i)))
                {
                    try
                    {
                        await SingleUploadCoreAsync(
                            data: data,
                            resourceId: resourceId,
                            remoteFilePath: remoteFilePath,

                            hostDeviceModel: hostDeviceModel,
                            hostDeviceManufacturer: hostDeviceManufacturer,

                            maxTriesCount: maxTriesPerUpload,
                            timeoutForUploadInMs: timeoutPerUploadInMs,
                            sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,
                            gracefulCancellationTimeoutInMs: gracefulCancellationTimeoutInMs,

                            autodisposeStream: autodisposeStreams,

                            initialMtuSize: initialMtuSize, //    both ios and android
                            pipelineDepth: pipelineDepth, //      ios only
                            byteAlignment: byteAlignment, //      ios only
                            windowCapacity: windowCapacity, //    android only
                            memoryAlignment: memoryAlignment //   android only
                        );

                        if (sleepTimeBetweenUploadsInMs > 0 && i < lastIndex) //we skip sleeping after the last upload
                        {
                            await Task.Delay(sleepTimeBetweenUploadsInMs);
                        }
                    }
                    catch (FileUploadErroredOutException)
                    {
                        if (moveToNextUploadInCaseOfError) //00
                        {
                            (filesThatFailedToBeUploaded ??= new List<string>(4)).Add(remoteFilePath);
                            continue;
                        }

                        throw;
                    }
                }

                return filesThatFailedToBeUploaded ?? Enumerable.Empty<string>();

                //00  we prefer to upload as many files as possible and report any failures collectively at the very end   we resorted to this
                //    tactic because failures are fairly common when uploading 50 files or more over to mcumgr devices, and we wanted to ensure
                //    that it would be as easy as possible to achieve the mass uploading just by using the default settings
            }
        }
    }
}
