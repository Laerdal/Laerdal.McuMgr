namespace Laerdal.McuMgr.FileUploader.Contracts.Native
{
    internal interface INativeFileUploaderQueryableProxy
    {
        string LastFatalErrorMessage { get; }
    }
}