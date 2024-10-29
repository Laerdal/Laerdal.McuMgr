using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.Common.Exceptions
{
    public class UnauthorizedException : Exception, IMcuMgrException
    {
        public string Resource { get; } = "";
        public EGlobalErrorCode GlobalErrorCode { get; }

        public UnauthorizedException(string nativeErrorMessage, EGlobalErrorCode globalErrorCode)
            : base($"Operation denied because it's not authorized: '{nativeErrorMessage}' (globalErrorCode={globalErrorCode})")
        {
            GlobalErrorCode = globalErrorCode;
        }

        public UnauthorizedException(string nativeErrorMessage, string resource)
            : base($"Operation denied on resource '{resource}' because it's not authorized: '{nativeErrorMessage}'")
        {
            Resource = resource;
        }
    }
}
