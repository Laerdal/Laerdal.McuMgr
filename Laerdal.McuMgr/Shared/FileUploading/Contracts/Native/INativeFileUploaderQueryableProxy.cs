namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    public interface INativeFileUploaderQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}