namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    public interface INativeFileUploaderCleanupProxy
    {
        void TryCleanupResourcesOfLastUpload();
    }
}
