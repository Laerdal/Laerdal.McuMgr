using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller.Contracts.Exceptions
{
    public class FirmwareInstallationErroredOutException : Exception, IFirmwareInstallationException
    {
        public EGlobalErrorCode GlobalErrorCode { get; } = EGlobalErrorCode.Unset;

        public FirmwareInstallationErroredOutException(string errorMessage, EGlobalErrorCode globalErrorCode = EGlobalErrorCode.Unset)
            : base($"An error occurred during firmware installation: '{errorMessage}' (globalErrorCode={globalErrorCode})")
        {
            GlobalErrorCode = globalErrorCode;
        }

        public FirmwareInstallationErroredOutException(string errorMessage, Exception innerException)
            : base($"An error occurred during firmware installation: '{errorMessage}'", innerException)
        {
        }
    }
}