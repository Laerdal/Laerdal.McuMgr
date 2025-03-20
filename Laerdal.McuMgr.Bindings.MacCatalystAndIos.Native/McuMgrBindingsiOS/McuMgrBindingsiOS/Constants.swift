internal struct Constants {
    @available(*, unavailable) private init() {}

    // theoretically the safe default value is cbPeripheral.maximumWriteValueLength(for: .withoutResponse) which returns 20
    // for under maccatalyst 18.1   but we realized during testing using version 1.9.2 that mtu=20 fails when we want to simply swap cached
    // firmwares and it is also dead slow for actual firmware uploads (only 5kb/sec compared to 12kb/sec which is what 1.9.0 could achieve)
    internal static let DefaultMtuForAssetUploading: Int = 57;

}
