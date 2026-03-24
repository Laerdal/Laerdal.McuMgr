package no.laerdal.mcumgr_laerdal_wrapper;

import io.runtime.mcumgr.McuMgrErrorCode;
import io.runtime.mcumgr.exception.McuMgrErrorException;
import io.runtime.mcumgr.response.HasReturnCode;

import java.util.Objects;

final class McuMgrExceptionHelpers
{
    public static String FormatErrorMessageWithExceptionTypeAndMessage(final String errorMessage, final Exception exception) {
        if (exception == null) {
            return String.format("[NativeErrorType: nil] %s", errorMessage);
        }

        if (!Objects.equals(errorMessage, exception.getLocalizedMessage())) {
            return String.format(
                    "[NativeErrorType: %s] %s: %s",
                    exception.getClass().getSimpleName(),
                    errorMessage,
                    exception.getLocalizedMessage()
            );
        }

        return String.format("[NativeErrorType: %s] %s", exception.getClass().getSimpleName(), errorMessage);
    }

    public static String FormatErrorMessageWithErrorCodes(final String errorMessage, final McuMgrErrorCode mcumgrErrorCode, final HasReturnCode.GroupReturnCode groupReturnCode) {
        return String.format("[McuMgrErrorCode: %s] [GroupReturnCode: %s] %s", mcumgrErrorCode, groupReturnCode, errorMessage);
    }

    // this method must be kept aligned between our ios lib and our android lib
    public static int DeduceGlobalErrorCodeFromException(final Exception exception)
    {
        if (!(exception instanceof McuMgrErrorException))
            return -99; // aka "unset / unknown error"

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
