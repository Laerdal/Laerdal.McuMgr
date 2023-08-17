// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileUploader.Contracts.Exceptions
{
    public sealed class UploadErroredOutRemoteFolderNotFoundException : UploadErroredOutException, IUploadRelatedException
    {
        public UploadErroredOutRemoteFolderNotFoundException(string remoteFolderPath) : base($"Remote folder-path '{remoteFolderPath}' doesn't exist")
        {
        }
    }
}