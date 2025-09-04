// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileDownloading.Contracts.Exceptions
{
    public sealed class AnotherFileDownloadIsAlreadyOngoingException : FileDownloadErroredOutException, IDownloadException
    {
        public AnotherFileDownloadIsAlreadyOngoingException(string nativeErrorMessage = "")
            : base(ProperlyFormatErrorMessage(nativeErrorMessage))
        {
        }

        static private string ProperlyFormatErrorMessage(string nativeErrorMessage)
        {
            const string prefix = "Another file-download operation is already ongoing";

            return string.IsNullOrWhiteSpace(nativeErrorMessage)
                ? prefix
                : $"{prefix}: {nativeErrorMessage}";
        }
    }
}
