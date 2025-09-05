// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public sealed class AnotherFileUploadIsAlreadyOngoingException : FileUploadErroredOutException, IUploadException
    {
        public AnotherFileUploadIsAlreadyOngoingException( //@formatter:off
            string remoteFilePath,
            string nativeErrorMessage = ""
        ) : base(remoteFilePath, ProperlyFormatErrorMessage(nativeErrorMessage)) //@formatter:on
        {
        }

        static private string ProperlyFormatErrorMessage(string nativeErrorMessage)
        {
            const string prefix = "Another file-upload operation is already ongoing";
            
            return string.IsNullOrWhiteSpace(nativeErrorMessage)
                ? prefix
                : $"{prefix}: {nativeErrorMessage}";
        }
    }
}
