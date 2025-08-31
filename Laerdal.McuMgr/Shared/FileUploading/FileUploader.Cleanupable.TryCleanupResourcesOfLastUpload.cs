namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public void TryCleanupResourcesOfLastUpload() => NativeFileUploaderProxy?.TryCleanupResourcesOfLastUpload();
    }
}
