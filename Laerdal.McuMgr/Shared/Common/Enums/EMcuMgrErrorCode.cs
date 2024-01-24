namespace Laerdal.McuMgr.Common.Enums
{
    public enum EMcuMgrErrorCode // must mirror io.runtime.mcumgr.McuMgrErrorCode  @formatter:off
    {
        Ok           = 00,
        Unknown      = 01,
        NoMemory     = 02,
        InValue      = 03,
        Timeout      = 04,
        NoEntry      = 05,
        BadState     = 06,
        TooLarge     = 07,
        NotSupported = 08,
        Corrupt      = 09,
        Busy         = 10,
        AccessDenied = 11,
        PerUser      = 256,
    } // @formatter:on
}
