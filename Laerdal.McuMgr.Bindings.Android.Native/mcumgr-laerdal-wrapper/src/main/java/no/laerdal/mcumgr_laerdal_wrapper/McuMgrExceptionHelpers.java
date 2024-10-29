package no.laerdal.mcumgr_laerdal_wrapper;

import io.runtime.mcumgr.McuMgrErrorCode;
import io.runtime.mcumgr.exception.McuMgrErrorException;
import io.runtime.mcumgr.response.HasReturnCode;

final class McuMgrExceptionHelpers
{
    // this method must be kept aligned between our ios lib and our android lib
    public static int DeduceGlobalErrorCodeFromException(final Exception exception)
    {
        if (!(exception instanceof McuMgrErrorException))
            return -99;

        McuMgrErrorException mcuMgrErrorException = (McuMgrErrorException) exception;
        McuMgrErrorCode exceptionCodeSpecs = mcuMgrErrorException.getCode();
        HasReturnCode.GroupReturnCode groupReturnCodeSpecs = mcuMgrErrorException.getGroupCode();

        return DeduceGlobalErrorCodeFromException(exceptionCodeSpecs, groupReturnCodeSpecs);
    }

    public static int DeduceGlobalErrorCodeFromException(final McuMgrErrorCode exceptionCodeSpecs, final HasReturnCode.GroupReturnCode groupReturnCodeSpecs)
    {
        return groupReturnCodeSpecs == null
               ? exceptionCodeSpecs.value() //                                             00
               : (((groupReturnCodeSpecs.group + 1) * 1000) + groupReturnCodeSpecs.rc); // 10

        //00  for auth errors and for nordic devices that do not support smp v2   these error codes occupy the range [0,999]
        //
        //10  for more evolved errors on nordic devices that do support smp v2    these error codes get mapped to values 1000+
        //
        //    in this way all error codes get mapped to a single human-readable enum in csharp called EGlobalErrorCode
    }
}
