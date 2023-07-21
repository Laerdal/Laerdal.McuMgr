import Foundation

// https://stackoverflow.com/a/49477937/863651
// https://learn.microsoft.com/en-us/xamarin/cross-platform/macios/binding/binding-types-reference
//
// note that at the time of this writing naming the interface as I<something> is vital for the bindings
// to be created properly by sharpie
//
@objc
public protocol IOSListenerForFirmwareInstaller {
    func cancelledAdvertisement()
    func logMessageAdvertisement(_ message: String, _ category: String, _ level: String)
    func fatalErrorOccurredAdvertisement(_ errorMessage: String)
    func stateChangedAdvertisement(_ oldState: EIOSFirmwareInstallationState, _ newState: EIOSFirmwareInstallationState)
    func busyStateChangedAdvertisement(_ busyNotIdle: Bool)
    func firmwareUploadProgressPercentageAndThroughputDataChangedAdvertisement(
            _ progressPercentage: Int,
            _ averageThroughput: Float32
    )
}
