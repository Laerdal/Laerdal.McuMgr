using System;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Exceptions
{
    public class FirmwareInstallationErroredOutException : Exception, IFirmwareInstallationException
    {
        public readonly EGlobalErrorCode GlobalErrorCode = EGlobalErrorCode.Unset;
        public readonly EFirmwareInstallerFatalErrorType FatalErrorType = EFirmwareInstallerFatalErrorType.Generic;

        protected FirmwareInstallationErroredOutException(string errorMessage)
            : base($"An error occurred during firmware installation: {errorMessage}", innerException: null)
        {
        }
        
        public FirmwareInstallationErroredOutException(string errorMessage, Exception innerException)
            : base($"An error occurred during firmware installation: {errorMessage}", innerException)
        {
        }
        
        public FirmwareInstallationErroredOutException(string errorMessage, EFirmwareInstallerFatalErrorType fatalErrorType = EFirmwareInstallerFatalErrorType.Generic, EGlobalErrorCode globalErrorCode = EGlobalErrorCode.Unset)
            : base($"An error occurred during firmware installation: {errorMessage} [fatalErrorType={fatalErrorType}] [globalErrorCode={globalErrorCode}]")
        {
            FatalErrorType = fatalErrorType;
            GlobalErrorCode = globalErrorCode;
        }
    }
}