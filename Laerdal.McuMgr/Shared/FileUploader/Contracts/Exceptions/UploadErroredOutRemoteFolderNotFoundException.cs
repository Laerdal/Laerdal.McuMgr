// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public sealed class UploadErroredOutRemoteFolderNotFoundException : UploadErroredOutException, IUploadException
    {       
        public UploadErroredOutRemoteFolderNotFoundException(string remoteFilePath)
            : base(remoteFilePath, $"One or more parent folders don't exist in remote file path '{remoteFilePath}'")
        {
        }
    }
}
