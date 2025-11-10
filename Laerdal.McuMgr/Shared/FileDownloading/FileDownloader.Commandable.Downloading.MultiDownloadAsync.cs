using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Helpers;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Exceptions;

namespace Laerdal.McuMgr.FileDownloading
{
    public partial class FileDownloader
    {
        public async Task<IDictionary<string, byte[]>> DownloadAsync( //@formatter:off
            IEnumerable<string> remoteFilePaths,
            string hostDeviceModel,
            string hostDeviceManufacturer,

            int timeoutPerDownloadInMs          = IFileDownloaderCommandable.Defaults.TimeoutPerDownloadInMs,
            int maxTriesPerDownload             = IFileDownloaderCommandable.Defaults.MaxTriesPerDownload,
            int sleepTimeBetweenRetriesInMs     = IFileDownloaderCommandable.Defaults.SleepTimeBetweenRetriesInMs,
            int gracefulCancellationTimeoutInMs = IFileDownloaderCommandable.Defaults.GracefulCancellationTimeoutInMs,
            
            ELogLevel? minimumNativeLogLevel          = null,

            int? initialMtuSize                 = null,
            int? windowCapacity                 = null,
            int? memoryAlignment                = null
        ) //@formatter:on
        {
            await EnsureExclusiveOperationTokenAsync().ConfigureAwait(false); //keep this outside of the try-finally block!

            try
            {
                ResetInternalStateTidbits();

                return await DownloadCoreAsync_();
            }
            finally
            {
                await ReleaseExclusiveOperationTokenAsync().ConfigureAwait(false);
            }

            async Task<IDictionary<string, byte[]>> DownloadCoreAsync_()
            {
                if (string.IsNullOrWhiteSpace(hostDeviceModel))
                    throw new ArgumentException("Host device model cannot be null or whitespace", nameof(hostDeviceModel));

                if (string.IsNullOrWhiteSpace(hostDeviceManufacturer))
                    throw new ArgumentException("Host device manufacturer cannot be null or whitespace", nameof(hostDeviceManufacturer));

                var sanitizedUniqueRemoteFilesPaths = RemoteFilePathHelpers.ValidateAndSanitizeRemoteFilePaths(remoteFilePaths);

                var results = sanitizedUniqueRemoteFilesPaths.ToDictionary(
                    keySelector: x => x,
                    elementSelector: _ => (byte[]) null
                );

                foreach (var path in sanitizedUniqueRemoteFilesPaths) //00 impossible to parallelize
                {
                    try
                    {
                        var data = await SingleDownloadCoreAsync(
                            remoteFilePath: path,
                            hostDeviceModel: hostDeviceModel,
                            hostDeviceManufacturer: hostDeviceManufacturer,
                            
                            minimumNativeLogLevel: minimumNativeLogLevel,

                            maxTriesCount: maxTriesPerDownload,
                            timeoutForDownloadInMs: timeoutPerDownloadInMs,
                            sleepTimeBetweenRetriesInMs: sleepTimeBetweenRetriesInMs,
                            gracefulCancellationTimeoutInMs: gracefulCancellationTimeoutInMs,

                            initialMtuSize: initialMtuSize,
                            windowCapacity: windowCapacity
                        );

                        results[path] = data;
                    }
                    catch (FileDownloadErroredOutException) //10
                    {
                        // the exception has already been logged so we just continue
                    }
                }

                return results;

                //00  we would love to parallelize all of this but the native side simply reverts to queuing the requests so its pointless
                //    nordic might fix this in the future but for now we have to do it sequentially
                //
                //10  we dont want to throw here because we want to return the results for the files that were successfully downloaded
                //    if a file fails to download we simply return null data for that file
            }
        }
    }
}
