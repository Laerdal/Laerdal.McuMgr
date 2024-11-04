using System;

namespace Laerdal.McuMgr.FileUploader.Contracts.Enums
{
    [Flags]
    public enum EFileUploaderVerdict //this must mirror the java enum values of E[Android|iOS]FileUploaderVerdict
    {
        Success = 0,
        FailedInvalidData = 1,
        FailedInvalidSettings = 2,
        FailedErrorUponCommencing = 3,
        FailedOtherUploadAlreadyInProgress = 4,
    }
}
