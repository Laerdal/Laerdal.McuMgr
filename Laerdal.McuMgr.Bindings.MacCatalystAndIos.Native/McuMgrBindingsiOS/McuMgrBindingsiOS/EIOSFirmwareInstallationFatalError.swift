@objc
public enum EIOSFirmwareInstallerFatalErrorType : Int //@formatter:off   this must mirror the enum values of E[Android|iOS]FirmwareInstallerFatalErrorType
{
        case                                       generic =  0
        case                 installationAlreadyInProgress =  1

        case                               invalidSettings =  2
        case                      givenFirmwareIsUnhealthy =  3 //dud firmware or failure to even unzip it to begin with
        case              installationInitializationFailed =  4 //start() call failed - could happen if our settings dont make sense

        case     firmwareExtendedDataIntegrityChecksFailed =  5 //native-state=validate    crc checks take place before the actual installation even starts
        case                   firmwareUploadingErroredOut =  6 //native-state=upload
        case  postInstallationDeviceHealthcheckTestsFailed =  7 //native-state=test
        case         postInstallationDeviceRebootingFailed =  8 //native-state=reset

        case             firmwareFinishingImageSwapTimeout =  9 //native-state=swap
        case    firmwarePostInstallationConfirmationFailed = 10 //native-state=confirm
}
