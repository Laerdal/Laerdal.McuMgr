internal struct Constants {
    @available(*, unavailable) private init() {}

    // [in versions >=1.9.3]
    // we intentionally reverted this to zero to disable it because as it turns out nordic somehow addressed this in versions 1.9.2 and
    // now the initial-mtu-size is being set automatically to a working value (450) in iPhones
    internal static let DefaultMtuForFileUploads: Int = 0; //80;

    internal static let DefaultMtuForFileDownloads: Int = 0; //80;

    //
    // [in versions <= 1.9.1]
    // theoretically the safe default value is cbPeripheral.maximumWriteValueLength(for: .withoutResponse) which returns 20
    // under maccatalyst 18.1   but we realized during testing using version 1.9.2 that mtu=20 fails when we want to simply swap cached
    // firmwares and it is also dead slow for actual firmware uploads (only 5kb/sec compared to 12kb/sec which is what 1.9.0 could achieve)
    //
    // moreover according to nordic folks:
    //
    // "We have set the MTU to a minimum of 73 for the following reason: When we updated the [iOS] library to send the Full SHA, this
    //  ballooned the minimum size of the MTU [to 73], otherwise the packet is bigger than the MTU, so no Data can be sent."
    //
    //  https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager/pull/338#issuecomment-3095856384
    //
    //  based on our own experiments though 73 sometimes causes the fw-upload process to hang and this is why we opted for a slightly higher value
    //
    // [in versions >=1.9.3]
    // we increased the value to 250 because of the above and also because even though Nordic claims to have fixed this issue, somehow the
    // default initial-mtu-value in 1.9.2+ is still just 20(!) for firmware installations which of course dooms the firmware installation altogether!
    //
    // note that the max-mtu-value=450 is working most of the times but it is "too edgy" and it sometimes fails
    //
    internal static let DefaultMtuForFirmwareInstallations: Int = 250;
}
