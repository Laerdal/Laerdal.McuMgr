namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    // ReSharper disable once RedundantExtendsListEntry
    public sealed class FileDownloadErroredOutAbruptlyDisconnectedException : FileDownloadErroredOutException, IDownloadException
    {
        public FileDownloadErroredOutAbruptlyDisconnectedException(string remoteFilePath) : base($"The device got abruptly disconnected while attempting to download file '{remoteFilePath}'")
        {
        }
    }
}