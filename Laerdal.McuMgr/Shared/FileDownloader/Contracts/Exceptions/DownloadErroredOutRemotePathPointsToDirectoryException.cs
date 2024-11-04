namespace Laerdal.McuMgr.FileDownloader.Contracts.Exceptions
{
    public sealed class DownloadErroredOutRemotePathPointsToDirectoryException : DownloadErroredOutException, IDownloadException
    {
        public DownloadErroredOutRemotePathPointsToDirectoryException(string remoteFilePath) : base($"The given file-path '{remoteFilePath}' points to a directory")
        {
        }
    }
}