namespace Laerdal.McuMgr.FileUploader.Contracts
{
    internal interface INativeFileUploaderCommandsProxy
    {
        string LastFatalErrorMessage { get; }

        void Cancel();
        void Disconnect();
        EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data);
    }
}