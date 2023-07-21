namespace Laerdal.McuMgr.FileDownloader.Exceptions
{
    public sealed class DownloadErroredOutRemoteFileNotFoundException : DownloadErroredOutException
    {
        public DownloadErroredOutRemoteFileNotFoundException(string remoteFilePath) : base($"The remote file '{remoteFilePath}' was not found")
        {
        }
    }
}