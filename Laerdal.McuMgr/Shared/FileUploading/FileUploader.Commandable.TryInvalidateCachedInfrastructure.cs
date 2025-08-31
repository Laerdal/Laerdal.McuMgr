namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TryInvalidateCachedInfrastructure() => NativeFileUploaderProxy?.TryInvalidateCachedInfrastructure() ?? false;
    }
}
