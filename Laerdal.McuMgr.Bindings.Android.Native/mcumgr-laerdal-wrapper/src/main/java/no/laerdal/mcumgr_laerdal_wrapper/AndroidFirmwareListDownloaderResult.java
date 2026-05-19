package no.laerdal.mcumgr_laerdal_wrapper;

@SuppressWarnings("unused")
public class AndroidFirmwareListDownloaderResult {
    private final EAndroidFirmwareListDownloaderVerdict _verdict;
    private final String _data;

    public AndroidFirmwareListDownloaderResult(final EAndroidFirmwareListDownloaderVerdict verdict, final String data) {
        _verdict = verdict;
        _data = data != null ? data : "";
    }

    public EAndroidFirmwareListDownloaderVerdict getVerdict() {
        return _verdict;
    }

    public String getData() {
        return _data;
    }
}

