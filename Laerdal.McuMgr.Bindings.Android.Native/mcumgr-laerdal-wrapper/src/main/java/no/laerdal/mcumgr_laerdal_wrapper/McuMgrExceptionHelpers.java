package no.laerdal.mcumgr_laerdal_wrapper;

import io.runtime.mcumgr.McuMgrErrorCode;
import io.runtime.mcumgr.exception.McuMgrErrorException;
import io.runtime.mcumgr.exception.McuMgrTimeoutException;
import io.runtime.mcumgr.response.HasReturnCode;

import java.util.Objects;

final class McuMgrExceptionHelpers {
    public static String FormatErrorMessageWithExceptionTypeAndMessage(final String errorMessage, final Exception exception) {
        if (exception == null) {
            return String.format("[NativeErrorType: nil] %s", errorMessage);
        }

        final String exceptionMessage = exception.getMessage() == null
                ? (exception.getCause() != null ? exception.getCause().getMessage() : null)
                : exception.getMessage();

        if (!Objects.equals(errorMessage, exceptionMessage)) {
            return String.format(
                    "[NativeErrorType: %s] %s: %s",
                    exception.getClass().getSimpleName(),
                    errorMessage == null ? "Error" : errorMessage,
                    exceptionMessage
            );
        }

        return String.format("[NativeErrorType: %s] %s", exception.getClass().getSimpleName(), errorMessage == null ? "(No native error available)" : errorMessage);
    }

    public static String FormatErrorMessageWithErrorCodes(final String errorMessage, final McuMgrErrorCode mcumgrErrorCode, final HasReturnCode.GroupReturnCode groupReturnCode) {
        return String.format("[McuMgrErrorCode: %s] [GroupReturnCode: %s] %s", mcumgrErrorCode, groupReturnCode, errorMessage);
    }

    // this method must be kept aligned between our ios lib and our android lib
    public static int DeduceGlobalErrorCodeFromException(final Exception exception, final boolean isConnectedNow) {
        if (exception instanceof McuMgrErrorException) {
            McuMgrErrorException mcuMgrErrorException = (McuMgrErrorException) exception;
            McuMgrErrorCode exceptionCodeSpecs = mcuMgrErrorException.getCode();
            HasReturnCode.GroupReturnCode groupReturnCodeSpecs = mcuMgrErrorException.getGroupCode();

            return DeduceGlobalErrorCodeFromException(groupReturnCodeSpecs, exceptionCodeSpecs);
        }
        else if (exception instanceof McuMgrTimeoutException) // == send-timeout
        {
            // this is to workaround to force the correct error-reporting (disconnected) when a device is abruptly disconnecting
            // pe due to running out of battery or going out of range for good (we typically get .sendTimeout in iOS which sucks
            // and doesnt help at all figuring out what has actually happened under the hood!)

            final int groupCode = 300; //groupReturnCodeSpecs_GroupCode  -> SubSystemMcuMgrTransport_*        these error codes must be aligned between c# / ios (McuMgrExceptionHelpers.swift) / android

            return isConnectedNow
                    ? DeduceGlobalErrorCodeFromException(
                    groupCode,
                    3, //                 groupReturnCodeSpecs_ReturnCode -> SendTimeout          these error codes must be aligned between c# / ios (McuMgrExceptionHelpers.swift) / android
                    McuMgrErrorCode.OK // exceptionCodeSpecs              -> Indifferent here
            )
                    : DeduceGlobalErrorCodeFromException( //hotfix to enforce the expected error-reporting behavior when the device disconnects abruptly
                    groupCode,
                    6, //                 groupReturnCodeSpecs_ReturnCode -> Disconnected         these error codes must be aligned between c# / ios (McuMgrExceptionHelpers.swift) / android
                    McuMgrErrorCode.OK // exceptionCodeSpecs              -> Indifferent here
            );
        }

        return -99; // aka "unset / unknown error"
    }

    public static int DeduceGlobalErrorCodeFromException(final HasReturnCode.GroupReturnCode groupReturnCodeSpecs, final McuMgrErrorCode exceptionCodeSpecs) {
        return DeduceGlobalErrorCodeFromException(
                groupReturnCodeSpecs == null ? null : groupReturnCodeSpecs.group,
                groupReturnCodeSpecs == null ? null : groupReturnCodeSpecs.rc,
                exceptionCodeSpecs
        );
    }

    public static int DeduceGlobalErrorCodeFromException(final Integer groupReturnCodeSpecs_GroupCode, final Integer groupReturnCodeSpecs_ReturnCode, final McuMgrErrorCode exceptionCodeSpecs) {
        return groupReturnCodeSpecs_GroupCode == null
                ? exceptionCodeSpecs.value() //                                                         00
                : (((groupReturnCodeSpecs_GroupCode + 1) * 1000) + groupReturnCodeSpecs_ReturnCode); // 10

        //00  for auth errors and for nordic devices that do not support smp v2   these error codes occupy the range [0,999]
        //
        //10  for more evolved errors on nordic devices that do support smp v2    these error codes get mapped to values 1000+
        //
        //    in this way all error codes get mapped to a single human-readable enum in csharp called EGlobalErrorCode
    }
}
