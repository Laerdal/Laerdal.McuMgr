package no.laerdal.mcumgr_laerdal_wrapper;

@SuppressWarnings("unused")
public class AndroidFirmwareListDownloaderResult {
    private final EAndroidFirmwareListDownloaderVerdict _verdict;
    private final String _jsonData;
    private final String _possibleErrorMessage;

    public AndroidFirmwareListDownloaderResult(final EAndroidFirmwareListDownloaderVerdict verdict, final String jsonData, final String possibleErrorMessage) {
        _verdict = verdict;
        _jsonData = jsonData != null ? jsonData : "";
        _possibleErrorMessage = possibleErrorMessage != null ? possibleErrorMessage : "";
    }

    public static AndroidFirmwareListDownloaderResult ForError(final EAndroidFirmwareListDownloaderVerdict verdict, final String possibleErrorMessage)
    {
        return new AndroidFirmwareListDownloaderResult(verdict, "", possibleErrorMessage);
    }

    public static AndroidFirmwareListDownloaderResult ForSuccess(final String jsonData)
    {
        return new AndroidFirmwareListDownloaderResult(EAndroidFirmwareListDownloaderVerdict.SUCCESS, jsonData, "");
    }

    public EAndroidFirmwareListDownloaderVerdict getVerdict() {
        return _verdict;
    }

    public String getJsonData() {
        return _jsonData;
    }

    public String getPossibleErrorMessage() {
        return _possibleErrorMessage;
    }
}
