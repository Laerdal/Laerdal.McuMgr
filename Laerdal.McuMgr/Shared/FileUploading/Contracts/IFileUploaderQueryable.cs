namespace Laerdal.McuMgr.FileUploading.Contracts
{
    public interface IFileUploaderQueryable
    {
        /// <summary>Holds the last error message emitted</summary>
        string LastFatalErrorMessage { get; }
    }
}