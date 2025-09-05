namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TrySetContext(object context) => NativeFileUploaderProxy?.TrySetContext(context) ?? false;
    }
}
