namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TryDisconnect() => NativeFileUploaderProxy?.TryDisconnect() ?? false;
    }
}
