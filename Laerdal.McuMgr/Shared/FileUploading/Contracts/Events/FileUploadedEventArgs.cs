using Laerdal.McuMgr.Common.Events;

namespace Laerdal.McuMgr.FileUploading.Contracts.Events
{
    public readonly struct FileUploadedEventArgs : IMcuMgrEventArgs //hotpathish
    {
        public readonly string Resource;

        public FileUploadedEventArgs(string resource)
        {
            Resource = resource;
        }
    }
}
