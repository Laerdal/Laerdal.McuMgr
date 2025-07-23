internal struct Constants {
    @available(*, unavailable) private init() {}

    // theoretically the safe default value is cbPeripheral.maximumWriteValueLength(for: .withoutResponse) which returns 20
    // for under maccatalyst 18.1   but we realized during testing using version 1.9.2 that mtu=20 fails when we want to simply swap cached
    // firmwares and it is also dead slow for actual firmware uploads (only 5kb/sec compared to 12kb/sec which is what 1.9.0 could achieve)
    //
    // moreover according to nordic folks:
    //
    // "We have set the MTU to a minimum of 73 for the following reason: When we updated the [iOS] library to send the Full SHA, this
    //  ballooned the minimum size of the MTU [to 73], otherwise the packet is bigger than the MTU, so no Data can be sent."
    //
    //  https://github.com/NordicSemiconductor/IOS-nRF-Connect-Device-Manager/pull/338#issuecomment-3095856384
    //
    internal static let DefaultMtuForAssetUploading: Int = 73;
}
