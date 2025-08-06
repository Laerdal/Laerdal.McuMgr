namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    internal interface INativeFileUploaderQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}