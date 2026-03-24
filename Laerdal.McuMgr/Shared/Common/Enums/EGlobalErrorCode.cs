namespace Laerdal.McuMgr.Common.Enums
{
    public enum EGlobalErrorCode //@formatter:off https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager/issues/201#issuecomment-2440126159
    {
        Unset   = -99, //this is our own to mark that we haven't received any error code from the device
        Generic =  -1, //in case the underlying native code receives an exception other than io.runtime.mcumgr.exception.*Error
        
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
        
        SubSystemDefault_Ok                             = (0 + 1) * 1_000 + 0,
        SubSystemDefault_Unknown                        = (0 + 1) * 1_000 + 1,
        SubSystemDefault_InvalidFormat                  = (0 + 1) * 1_000 + 2,
        SubSystemDefault_QueryYieldsNoAnswer            = (0 + 1) * 1_000 + 3,

        SubSystemImage_Ok                               = (1 + 1) * 1_000 + 00,
        SubSystemImage_Unknown                          = (1 + 1) * 1_000 + 01,
        SubSystemImage_FlashConfigQueryFail             = (1 + 1) * 1_000 + 02,
        SubSystemImage_NoImage                          = (1 + 1) * 1_000 + 03,
        SubSystemImage_NoTlvs                           = (1 + 1) * 1_000 + 04,
        SubSystemImage_InvalidTlv                       = (1 + 1) * 1_000 + 05,
        SubSystemImage_TlvMultipleHashesFound           = (1 + 1) * 1_000 + 06,
        SubSystemImage_TlvInvalidSize                   = (1 + 1) * 1_000 + 07,
        SubSystemImage_HashNotFound                     = (1 + 1) * 1_000 + 08,
        SubSystemImage_NoFreeSlot                       = (1 + 1) * 1_000 + 09,
        SubSystemImage_FlashOpenFailed                  = (1 + 1) * 1_000 + 10,
        SubSystemImage_FlashReadFailed                  = (1 + 1) * 1_000 + 11,
        SubSystemImage_FlashWriteFailed                 = (1 + 1) * 1_000 + 12,
        SubSystemImage_FlashEraseFailed                 = (1 + 1) * 1_000 + 13,
        SubSystemImage_InvalidSlot                      = (1 + 1) * 1_000 + 14,
        SubSystemImage_NoFreeMemory                     = (1 + 1) * 1_000 + 15,
        SubSystemImage_FlashContextAlreadySet           = (1 + 1) * 1_000 + 16,
        SubSystemImage_FlashContextNotSet               = (1 + 1) * 1_000 + 17,
        SubSystemImage_FlashAreaDeviceNull              = (1 + 1) * 1_000 + 18,
        SubSystemImage_InvalidPageOffset                = (1 + 1) * 1_000 + 19,
        SubSystemImage_InvalidOffset                    = (1 + 1) * 1_000 + 20,
        SubSystemImage_InvalidLength                    = (1 + 1) * 1_000 + 21,
        SubSystemImage_InvalidImageHeader               = (1 + 1) * 1_000 + 22,
        SubSystemImage_InvalidImageHeaderMagic          = (1 + 1) * 1_000 + 23,
        SubSystemImage_InvalidHash                      = (1 + 1) * 1_000 + 24,
        SubSystemImage_InvalidFlashAddress              = (1 + 1) * 1_000 + 25,
        SubSystemImage_VersionGetFailed                 = (1 + 1) * 1_000 + 26,
        SubSystemImage_CurrentVersionIsNewer            = (1 + 1) * 1_000 + 27,
        SubSystemImage_ImageAlreadyPending              = (1 + 1) * 1_000 + 28,
        SubSystemImage_InvalidImageVectorTable          = (1 + 1) * 1_000 + 29,
        SubSystemImage_InvalidImageTooLarge             = (1 + 1) * 1_000 + 30,
        SubSystemImage_InvalidImageDataOverrun          = (1 + 1) * 1_000 + 31,
        SubSystemImage_ImageConfirmationDenied          = (1 + 1) * 1_000 + 32,
        SubSystemImage_ImageSettingTestToActiveDenied   = (1 + 1) * 1_000 + 33,

        SubSystemStats_Ok                               = (2 + 1) * 1_000 + 0,
        SubSystemStats_Unknown                          = (2 + 1) * 1_000 + 1,
        SubSystemStats_InvalidGroupName                 = (2 + 1) * 1_000 + 2,
        SubSystemStats_InvalidStatName                  = (2 + 1) * 1_000 + 3,
        SubSystemStats_InvalidStatSize                  = (2 + 1) * 1_000 + 4,
        SubSystemStats_WalkAborted                      = (2 + 1) * 1_000 + 5,

        SubSystemSettings_Ok                            = (3 + 1) * 1_000 + 0,
        SubSystemSettings_Unknown                       = (3 + 1) * 1_000 + 1,
        SubSystemSettings_KeyTooLong                    = (3 + 1) * 1_000 + 2,
        SubSystemSettings_KeyNotFound                   = (3 + 1) * 1_000 + 3,
        SubSystemSettings_ReadNotSupported              = (3 + 1) * 1_000 + 4,
        SubSystemSettings_RootKeyNotFound               = (3 + 1) * 1_000 + 5,
        SubSystemSettings_WriteNotSupported             = (3 + 1) * 1_000 + 6,
        SubSystemSettings_DeleteNotSupported            = (3 + 1) * 1_000 + 7,

        // int GROUP_LOGS   = 4 // these group-ids are specific to nordic and are not supported by zephyr
        // int GROUP_CRASH  = 5 // so nordic did not migrate these to smp v2   so they do not support
        // int GROUP_SPLIT  = 6 // group-errors   at least not out-of-the-box
        // int GROUP_RUN    = 7

        SubSystemFilesystem_Ok                          = (8 + 1) * 1_000 + 00,
        SubSystemFilesystem_Unknown                     = (8 + 1) * 1_000 + 01,
        SubSystemFilesystem_InvalidName                 = (8 + 1) * 1_000 + 02,
        SubSystemFilesystem_NotFound                    = (8 + 1) * 1_000 + 03, //when trying to download a file that doesnt exist on the remote device
        SubSystemFilesystem_IsDirectory                 = (8 + 1) * 1_000 + 04,
        SubSystemFilesystem_OpenFailed                  = (8 + 1) * 1_000 + 05,
        SubSystemFilesystem_SeekFailed                  = (8 + 1) * 1_000 + 06,
        SubSystemFilesystem_ReadFailed                  = (8 + 1) * 1_000 + 07,
        SubSystemFilesystem_TruncateFailed              = (8 + 1) * 1_000 + 08,
        SubSystemFilesystem_DeleteFailed                = (8 + 1) * 1_000 + 09,
        SubSystemFilesystem_WriteFailed                 = (8 + 1) * 1_000 + 10,
        SubSystemFilesystem_OffsetNotValid              = (8 + 1) * 1_000 + 11,
        SubSystemFilesystem_OffsetLargerThanFile        = (8 + 1) * 1_000 + 12,
        SubSystemFilesystem_ChecksumHashNotFound        = (8 + 1) * 1_000 + 13,
        SubSystemFilesystem_MountPointNotFound          = (8 + 1) * 1_000 + 14,
        SubSystemFilesystem_ReadOnlyFilesystem          = (8 + 1) * 1_000 + 15,
        SubSystemFilesystem_FileEmpty                   = (8 + 1) * 1_000 + 16,

        SubSystemShell_Ok                               = (9 + 1) * 1_000 + 0,
        SubSystemShell_Unknown                          = (9 + 1) * 1_000 + 1,
        SubSystemShell_CommandTooLong                   = (9 + 1) * 1_000 + 2,
        SubSystemShell_EmptyCommand                     = (9 + 1) * 1_000 + 3,
        
        SubSystemFileTransporter_GenericError                = (200 + 1) * 1_000 + 0, //this is for the legacy FileTransferError in iOS that doesnt derive from zephyr
        SubSystemFileTransporter_InvalidData                 = (200 + 1) * 1_000 + 1,
        SubSystemFileTransporter_InvalidPayload              = (200 + 1) * 1_000 + 2,
        SubSystemFileTransporter_MissingUploadConfiguration  = (200 + 1) * 1_000 + 3,

        SubSystemMcuMgrTransport_GenericError                                  = (300 + 1) * 1_000 + 0, // native transport errors must be mapped in a special
        SubSystemMcuMgrTransport_BadHeader                                     = (300 + 1) * 1_000 + 1,
        SubSystemMcuMgrTransport_SendFailed                                    = (300 + 1) * 1_000 + 2,
        SubSystemMcuMgrTransport_SendTimeout                                   = (300 + 1) * 1_000 + 3, // we get a send-timeout on abrupt disconnection due to the remote device going out of range or out of battery
        SubSystemMcuMgrTransport_BadChunking                                   = (300 + 1) * 1_000 + 4,
        SubSystemMcuMgrTransport_BadResponse                                   = (300 + 1) * 1_000 + 5,
        SubSystemMcuMgrTransport_Disconnected                                  = (300 + 1) * 1_000 + 6, // we would expect to get this but upon abrupt disconnection but we dont get it at all   we get SubSystemMcuMgrTransport_SendTimeout instead
        SubSystemMcuMgrTransport_WaitAndRetry                                  = (300 + 1) * 1_000 + 7,
        SubSystemMcuMgrTransport_InsufficientMtu                               = (300 + 1) * 1_000 + 8,
        SubSystemMcuMgrTransport_ConnectionFailed                              = (300 + 1) * 1_000 + 9,
        SubSystemMcuMgrTransport_ConnectionTimeout                             = (300 + 1) * 1_000 + 10,
        SubSystemMcuMgrTransport_PeripheralNotReadyForWriteWithoutResponse     = (300 + 1) * 1_000 + 11,
    }
}
