namespace Laerdal.McuMgr.FileUploader.Exceptions
{
    public sealed class UploadErroredOutRemoteFolderNotFoundException : UploadErroredOutException
    {
        public UploadErroredOutRemoteFolderNotFoundException(string remoteFolderPath) : base($"Remote folder-path '{remoteFolderPath}' doesn't exist")
        {
        }
    }
}