using System;
using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.Common.Exceptions
{
    public class UnauthorizedException : Exception, IMcuMgrException //todo   get rid of this once we refactor all classes to use their own unauthorized exceptions
    {
        public string Resource { get; } = "";

        public UnauthorizedException(string nativeErrorMessage)
            : base($"Operation denied because it's not authorized: '{nativeErrorMessage}'")
        {
        }

        public UnauthorizedException(string nativeErrorMessage, string resource)
            : base($"Operation denied on resource '{resource}' because it's not authorized: '{nativeErrorMessage}'")
        {
            Resource = resource;
        }
    }
}
