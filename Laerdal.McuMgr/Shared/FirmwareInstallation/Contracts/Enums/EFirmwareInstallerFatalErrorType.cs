namespace Laerdal.McuMgr.FirmwareInstallation.Contracts.Enums
{
    public enum EFirmwareInstallerFatalErrorType //@formatter:off   these must mirror the enum values of E[Android|IOS]FirmwareInstallerFatalErrorType
    {
                                               Generic =  0, //native-state=none
                         InstallationAlreadyInProgress =  1, //native-state=none

                                       InvalidSettings =  2, //native-state=none
                            DeviceDisconnectedAbruptly =  3, //native-state=none
                            GivenFirmwareDataUnhealthy =  4, //native-state=none   dud firmware or failure to even unzip it to begin with
                      InstallationInitializationFailed =  5, //native-state=none   start() call failed - could happen if our settings dont make sense

             FirmwareExtendedDataIntegrityChecksFailed =  6, //native-state=validate    crc checks take place before the actual installation even starts
                           FirmwareUploadingErroredOut =  7, //native-state=upload
          PostInstallationDeviceHealthcheckTestsFailed =  8, //native-state=test
                 PostInstallationDeviceRebootingFailed =  9, //native-state=reset

                     FirmwareFinishingImageSwapTimeout = 10, //native-state=swap
            FirmwarePostInstallationConfirmationFailed = 11, //native-state=confirm
    }
}
