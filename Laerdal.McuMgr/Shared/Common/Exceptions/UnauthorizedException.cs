using System;

namespace Laerdal.McuMgr.Common.Exceptions
{
    public class UnauthorizedException : Exception, IMcuMgrException
    {
        public UnauthorizedException() : base("Operation denied because it's not authorized")
        {
        }
    }
}
