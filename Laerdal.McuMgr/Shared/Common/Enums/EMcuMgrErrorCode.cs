namespace Laerdal.McuMgr.Common.Enums
{
    public enum EMcuMgrErrorCode // must mirror io.runtime.mcumgr.McuMgrErrorCode  @formatter:off
    {
        Unset        = -99, //this is our own to mark that we haven't received any error code from the device
        Ok           = 000,
        Unknown      = 001,
        NoMemory     = 002,
        InValue      = 003,
        Timeout      = 004,
        NoEntry      = 005,
        BadState     = 006,
        TooLarge     = 007,
        NotSupported = 008,
        Corrupt      = 009,
        Busy         = 010,
        AccessDenied = 011,
        PerUser      = 256,
    } // @formatter:on
}
