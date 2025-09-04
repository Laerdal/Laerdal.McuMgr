// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public sealed class FileDownloadErroredOutRemoteFileNotFoundException : FileDownloadErroredOutException, IDownloadException
    {
        public FileDownloadErroredOutRemoteFileNotFoundException(string remoteFilePath) : base($"The remote file '{remoteFilePath}' was not found")
        {
        }
    }
}