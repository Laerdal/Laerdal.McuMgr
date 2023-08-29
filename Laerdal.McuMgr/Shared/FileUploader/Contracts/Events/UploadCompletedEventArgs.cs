namespace Laerdal.McuMgr.FileUploader.Contracts.Events
{
    public readonly struct UploadCompletedEventArgs
    {
        public string Resource { get; }

        public UploadCompletedEventArgs(string resource)
        {
            Resource = resource;
        }
    }
}