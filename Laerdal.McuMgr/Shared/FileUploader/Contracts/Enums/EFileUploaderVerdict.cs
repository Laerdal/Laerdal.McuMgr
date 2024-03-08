using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Enums
{
    [Flags]
    public enum EFileUploaderVerdict //this must mirror the java enum values of E[Android|iOS]FileUploaderVerdict
    {
        Success = 0,
        FailedInvalidSettings = 1,
        FailedInvalidData = 2,
        FailedOtherUploadAlreadyInProgress = 3,
    }
}
