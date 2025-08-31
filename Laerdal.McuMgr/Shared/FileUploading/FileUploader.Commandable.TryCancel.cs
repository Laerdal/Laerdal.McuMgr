// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TryCancel(string reason = "")
        {
            IsCancellationRequested = true; //order

            var success = NativeFileUploaderProxy?.TryCancel(reason) ?? false; //order

            KeepGoing.Set(); //order   unblocks any ongoing installation/verification so that it can observe the cancellation

            return success;
        }
    }
}
