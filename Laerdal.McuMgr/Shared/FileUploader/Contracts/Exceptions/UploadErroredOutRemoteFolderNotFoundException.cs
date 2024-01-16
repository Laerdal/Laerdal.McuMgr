// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public sealed class UploadErroredOutRemoteFolderNotFoundException : UploadErroredOutException, IUploadException
    {
        public string RemoteFolderPath { get; }
        
        public UploadErroredOutRemoteFolderNotFoundException(string remoteFolderPath) : base($"Remote folder-path '{remoteFolderPath}' doesn't exist")
        {
            RemoteFolderPath = remoteFolderPath;
        }
    }
}
