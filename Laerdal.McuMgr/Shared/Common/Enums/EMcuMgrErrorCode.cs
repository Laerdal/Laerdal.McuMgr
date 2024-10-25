namespace Laerdal.McuMgr.Common.Enums
{
    // must mirror  mcumgr-core/src/main/java/io/runtime/mcumgr/exception/McuMgrErrorException.java
    public enum EMcuMgrErrorCode // @formatter:off
    {
        Unset                 = -99, //this is our own to mark that we haven't received any error code from the device
        Generic               =  -1, //in case the underlying native code receives an exception other than io.runtime.mcumgr.exception.McuMgrErrorException
        Ok                    = 000,
        Unknown               = 001, //when uploading files to the device this error code means that the target file-path has one or more non-existent directories in it
        NoMemory              = 002,
        InValue               = 003,
        Timeout               = 004,
        NoEntry               = 005,
        BadState              = 006,
        TooLarge              = 007,
        NotSupported          = 008,
        Corrupt               = 009,
        Busy                  = 010,
        AccessDenied          = 011,
        ProtocolVersionTooOld = 012,
        ProtocolVersionTooNew = 013,
        PerUser               = 256,
    } // @formatter:on
}
