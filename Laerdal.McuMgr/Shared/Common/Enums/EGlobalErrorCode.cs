namespace Laerdal.McuMgr.Common.Enums
{
    public enum EGlobalErrorCode //@formatter:off https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager/issues/201#issuecomment-2440126159
    {
        Unset   = -99, //this is our own to mark that we haven't received any error code from the device
        Generic =  -1, //in case the underlying native code receives an exception other than io.runtime.mcumgr.exception.McuMgrErrorException
        
        // must mirror  mcumgr-core/src/main/java/io/runtime/mcumgr/exception/McuMgrErrorException.java
        McuMgrErrorBeforeSmpV2_Ok                       =  000,
        McuMgrErrorBeforeSmpV2_Unknown                  =  001, //when uploading files to the device this error code means that the target file-path has one or more non-existent directories in it
        McuMgrErrorBeforeSmpV2_NoMemory                 =  002,
        McuMgrErrorBeforeSmpV2_InValue                  =  003,
        McuMgrErrorBeforeSmpV2_Timeout                  =  004,
        McuMgrErrorBeforeSmpV2_NoEntry                  =  005,
        McuMgrErrorBeforeSmpV2_BadState                 =  006,
        McuMgrErrorBeforeSmpV2_TooLarge                 =  007,
        McuMgrErrorBeforeSmpV2_NotSupported             =  008,
        McuMgrErrorBeforeSmpV2_Corrupt                  =  009,
        McuMgrErrorBeforeSmpV2_Busy                     =  010,
        McuMgrErrorBeforeSmpV2_AccessDenied             =  011,
        McuMgrErrorBeforeSmpV2_ProtocolVersionTooOld    =  012,
        McuMgrErrorBeforeSmpV2_ProtocolVersionTooNew    =  013,
        McuMgrErrorBeforeSmpV2_PerUser                  =  256,
        
        SubSystemDefault_Ok                             = 1000,
        SubSystemDefault_Unknown                        = 1001,
        SubSystemDefault_InvalidFormat                  = 1002,
        SubSystemDefault_QueryYieldsNoAnswer            = 1003,

        SubSystemImage_Ok                               = 2000,
        SubSystemImage_Unknown                          = 2001,
        SubSystemImage_FlashConfigQueryFail             = 2002,
        SubSystemImage_NoImage                          = 2003,
        SubSystemImage_NoTlvs                           = 2004,
        SubSystemImage_InvalidTlv                       = 2005,
        SubSystemImage_TlvMultipleHashesFound           = 2006,
        SubSystemImage_TlvInvalidSize                   = 2007,
        SubSystemImage_HashNotFound                     = 2008,
        SubSystemImage_NoFreeSlot                       = 2009,
        SubSystemImage_FlashOpenFailed                  = 2010,
        SubSystemImage_FlashReadFailed                  = 2011,
        SubSystemImage_FlashWriteFailed                 = 2012,
        SubSystemImage_FlashEraseFailed                 = 2013,
        SubSystemImage_InvalidSlot                      = 2014,
        SubSystemImage_NoFreeMemory                     = 2015,
        SubSystemImage_FlashContextAlreadySet           = 2016,
        SubSystemImage_FlashContextNotSet               = 2017,
        SubSystemImage_FlashAreaDeviceNull              = 2018,
        SubSystemImage_InvalidPageOffset                = 2019,
        SubSystemImage_InvalidOffset                    = 2020,
        SubSystemImage_InvalidLength                    = 2021,
        SubSystemImage_InvalidImageHeader               = 2022,
        SubSystemImage_InvalidImageHeaderMagic          = 2023,
        SubSystemImage_InvalidHash                      = 2024,
        SubSystemImage_InvalidFlashAddress              = 2025,
        SubSystemImage_VersionGetFailed                 = 2026,
        SubSystemImage_CurrentVersionIsNewer            = 2027,
        SubSystemImage_ImageAlreadyPending              = 2028,
        SubSystemImage_InvalidImageVectorTable          = 2029,
        SubSystemImage_InvalidImageTooLarge             = 2030,
        SubSystemImage_InvalidImageDataOverrun          = 2031,
        SubSystemImage_ImageConfirmationDenied          = 2032,
        SubSystemImage_ImageSettingTestToActiveDenied   = 2033,

        SubSystemStats_Ok                               = 3000,
        SubSystemStats_Unknown                          = 3001,
        SubSystemStats_InvalidGroupName                 = 3002,
        SubSystemStats_InvalidStatName                  = 3003,
        SubSystemStats_InvalidStatSize                  = 3004,
        SubSystemStats_WalkAborted                      = 3005,

        SubSystemSettings_Ok                            = 4000,
        SubSystemSettings_Unknown                       = 4001,
        SubSystemSettings_KeyTooLong                    = 4002,
        SubSystemSettings_KeyNotFound                   = 4003,
        SubSystemSettings_ReadNotSupported              = 4004,
        SubSystemSettings_RootKeyNotFound               = 4005,
        SubSystemSettings_WriteNotSupported             = 4006,
        SubSystemSettings_DeleteNotSupported            = 4007,

        // int GROUP_LOGS   = 4 // these group-ids are specific to nordic and are not supported by zephyr
        // int GROUP_CRASH  = 5 // so nordic did not migrate these to smp v2   so they do not support
        // int GROUP_SPLIT  = 6 // group-errors   at least not out-of-the-box
        // int GROUP_RUN    = 7

        SubSystemFilesystem_Ok                          = 9000,
        SubSystemFilesystem_Unknown                     = 9001,
        SubSystemFilesystem_InvalidName                 = 9002,
        SubSystemFilesystem_NotFound                    = 9003,
        SubSystemFilesystem_IsDirectory                 = 9004,
        SubSystemFilesystem_OpenFailed                  = 9005,
        SubSystemFilesystem_SeekFailed                  = 9006,
        SubSystemFilesystem_ReadFailed                  = 9007,
        SubSystemFilesystem_TruncateFailed              = 9008,
        SubSystemFilesystem_DeleteFailed                = 9009,
        SubSystemFilesystem_WriteFailed                 = 9010,
        SubSystemFilesystem_OffsetNotValid              = 9011,
        SubSystemFilesystem_OffsetLargerThanFile        = 9012,
        SubSystemFilesystem_ChecksumHashNotFound        = 9013,
        SubSystemFilesystem_MountPointNotFound          = 9014,
        SubSystemFilesystem_ReadOnlyFilesystem          = 9015,
        SubSystemFilesystem_FileEmpty                   = 9016,

        SubSystemShell_Ok                               = 10000,
        SubSystemShell_Unknown                          = 10001,
        SubSystemShell_CommandTooLong                   = 10002,
        SubSystemShell_EmptyCommand                     = 10003,
    }
}