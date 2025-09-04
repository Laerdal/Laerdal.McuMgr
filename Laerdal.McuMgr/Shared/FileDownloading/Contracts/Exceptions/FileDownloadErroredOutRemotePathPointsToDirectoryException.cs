namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public sealed class FileDownloadErroredOutRemotePathPointsToDirectoryException : FileDownloadErroredOutException, IDownloadException
    {
        public FileDownloadErroredOutRemotePathPointsToDirectoryException(string remoteFilePath) : base($"The given file-path '{remoteFilePath}' points to a directory")
        {
        }
    }
}