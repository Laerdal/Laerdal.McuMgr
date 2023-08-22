using Laerdal.McuMgr.FileUploader.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploader.Contracts.Native
{
    internal interface INativeFileUploaderCommandsProxy
    {
        string LastFatalErrorMessage { get; }

        void Cancel();
        void Disconnect();
        EFileUploaderVerdict BeginUpload(string remoteFilePath, byte[] data);
    }
}