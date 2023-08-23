namespace Laerdal.McuMgr.FileUploader.Contracts
{
    public interface IFileUploaderQueryable
    {
        /// <summary>Holds the last error message emitted</summary>
        string LastFatalErrorMessage { get; }
    }
}