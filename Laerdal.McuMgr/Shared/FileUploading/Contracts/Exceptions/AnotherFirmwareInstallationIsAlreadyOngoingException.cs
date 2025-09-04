// ReSharper disable once RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileUploading.Contracts.Exceptions
{
    public sealed class AnotherFileUploadIsAlreadyOngoingException : FileUploadErroredOutException, IUploadException
    {
        public AnotherFileUploadIsAlreadyOngoingException( //@formatter:off
            string remoteFilePath,
            string nativeErrorMessage = ""
        ) //@formatter:on
            : base(remoteFilePath, ProperlyFormatErrorMessage(nativeErrorMessage))
        {
        }

        static private string ProperlyFormatErrorMessage(string nativeErrorMessage)
        {
            const string prefix = "Another file-upload operation is already ongoing";
            return string.IsNullOrWhiteSpace(nativeErrorMessage)
                ? prefix
                : $"Another firmware installation is already ongoing: {nativeErrorMessage}";
        }
    }
}
