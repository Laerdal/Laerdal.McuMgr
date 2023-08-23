// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public sealed class DownloadErroredOutRemoteFileNotFoundException : DownloadErroredOutException, IDownloadException
    {
        public DownloadErroredOutRemoteFileNotFoundException(string remoteFilePath) : base($"The remote file '{remoteFilePath}' was not found")
        {
        }
    }
}