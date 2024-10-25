package no.laerdal.mcumgr_laerdal_wrapper;

import io.runtime.mcumgr.exception.McuMgrErrorException;
import io.runtime.mcumgr.response.HasReturnCode;

final class McuMgrExceptionHelpers {

    final protected static class ErrorCodes {
        public final int errorCode; //      typically EMcuMgrErrorCode
        public final int errorGroupCode; // typically a *.ReturnCode (like FsManager.ReturnCode or ImageManage.ReturnCode and so on)

        private ErrorCodes(int errorCode, int errorGroupCode) {
            this.errorCode = errorCode;
            this.errorGroupCode = errorGroupCode;
        }
    }

    public static ErrorCodes DeduceErrorCodesFromException(Exception exception) {
        if (!(exception instanceof McuMgrErrorException))
            return new ErrorCodes(-99, -99);

        McuMgrErrorException mcuMgrErrorException = (McuMgrErrorException) exception;
        HasReturnCode.GroupReturnCode groupReturnCode = mcuMgrErrorException.getGroupCode();

        int errorCode = mcuMgrErrorException.getCode().value();

        int errorGroupCode = groupReturnCode != null
                ? groupReturnCode.rc
                : -99;

        return new ErrorCodes(errorCode, errorGroupCode);
    }
}