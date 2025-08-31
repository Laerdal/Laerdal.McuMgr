namespace Laerdal.McuMgr.FileUploading.Contracts
{
    public interface IFileUploaderCleanupable
    {
        void TryCleanupResourcesOfLastUpload();
    }
}