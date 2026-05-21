package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;

import androidx.annotation.NonNull;

import org.jetbrains.annotations.Contract;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.managers.ImageManager;
import io.runtime.mcumgr.response.img.McuMgrImageStateResponse;

@SuppressWarnings({"unused", "DuplicatedCode"})
public class AndroidFirmwareListDownloader {
    private EAndroidLoggingLevel _minimumNativeLogLevel = EAndroidLoggingLevel.Error;

    private Context _context;
    private BluetoothDevice _bluetoothDevice;

    private ImageManager _imageManager;
    private McuMgrBleTransport _transport;

    private final ExecutorService _backgroundExecutor = Executors.newCachedThreadPool();

    public AndroidFirmwareListDownloader() {
    }

    public AndroidFirmwareListDownloader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice) {
        _context = context;
        _bluetoothDevice = bluetoothDevice;
    }

    public boolean trySetContext(@NonNull final Context context) {
        if (!tryInvalidateCachedInfrastructure()) //order
            return false;

        _context = context;
        return true;
    }

    public boolean trySetBluetoothDevice(@NonNull final BluetoothDevice bluetoothDevice) {
        if (!tryInvalidateCachedInfrastructure()) //order
        {
            logInBg("[AFLD.TSBD.020] Failed to invalidate the cached-transport instance", EAndroidLoggingLevel.Error);
            return false;
        }

        _bluetoothDevice = bluetoothDevice; //order

        logInBg("[AFLD.TSBD.030] Successfully set the android-bluetooth-device to the given value", EAndroidLoggingLevel.Trace);

        return true;
    }

    public boolean tryInvalidateCachedInfrastructure() {
        return tryDisposeTransport(); //         order
    }

    public AndroidFirmwareListDownloaderResult tryGetImageListJson(
            final int initialMtuSize,
            final int minimumNativeLogLevelNumeric
    ) {
        if (_context == null) {
            logInBg("[AFLD.GILJ.010] Provided context is null", EAndroidLoggingLevel.Error);

            return AndroidFirmwareListDownloaderResult.ForError(EAndroidFirmwareListDownloaderVerdict.FAILED__INVALID_SETTINGS, "Provided context is null");
        }

        if (_bluetoothDevice == null) {
            logInBg("[AFLD.GILJ.020] Provided bluetooth-device is null", EAndroidLoggingLevel.Error);

            return AndroidFirmwareListDownloaderResult.ForError(EAndroidFirmwareListDownloaderVerdict.FAILED__INVALID_SETTINGS, "Provided native-bluetooth-device is null");
        }

        _minimumNativeLogLevel = McuMgrLogLevelHelpers.translateLogLevel(minimumNativeLogLevelNumeric);

        try {
            ensureTransportIsInitializedExactlyOnce(initialMtuSize); //order
            setLoggingEnabledOnTransport(false); //order

            final EAndroidFirmwareListDownloaderVerdict verdict = ensureImageManagerIsInitializedExactlyOnce(); //order
            if (verdict != EAndroidFirmwareListDownloaderVerdict.SUCCESS)
                return AndroidFirmwareListDownloaderResult.ForError(verdict, "Failed to initialize mcumgr-image-manager with the given settings");

            return tryParseInformation(_imageManager.list());

        } catch (final Exception ex) {
            return AndroidFirmwareListDownloaderResult.ForError(EAndroidFirmwareListDownloaderVerdict.FAILED__INVALID_DATA, "An unexpected error occurred:\n\n" + ex);

        } finally {
            tryDisconnect();
        }
    }

    @NonNull
    private static String bytesToHex(byte[] bytes) {
        if (bytes == null) return "";

        final StringBuilder sb = new StringBuilder(bytes.length * 2);
        for (byte b : bytes) sb.append(String.format("%02x", b));

        return sb.toString();
    }

    @NonNull
    private static AndroidFirmwareListDownloaderResult tryParseInformation(McuMgrImageStateResponse response) {
        try {
            JSONArray firmwareArray = new JSONArray();
            for (McuMgrImageStateResponse.ImageSlot image : response.images) {
                JSONObject firmwareObject = new JSONObject();
                firmwareObject.put("version", image.version);
                firmwareObject.put("slot", image.slot);
                firmwareObject.put("active", image.active);
                firmwareObject.put("bootable", image.bootable);
                firmwareObject.put("compressed", image.compressed);
                firmwareObject.put("confirmed", image.confirmed);
                firmwareObject.put("image_number", image.image);
                firmwareObject.put("permanent", image.permanent);
                firmwareObject.put("pending", image.pending);
                firmwareObject.put("hash", bytesToHex(image.hash));
                firmwareArray.put(firmwareObject);
            }

            return AndroidFirmwareListDownloaderResult.ForSuccess(firmwareArray.toString(4));
        } catch (JSONException ex) {
            return AndroidFirmwareListDownloaderResult.ForError(EAndroidFirmwareListDownloaderVerdict.FAILED__INVALID_DATA, "Failed to package McuMgrImageStateResponse as json:\n\n" + ex);
        }
    }

    @SuppressWarnings("UnusedReturnValue")
    public boolean tryDisconnect() {
        logInBg("[AFLD.TDISC.010] Will try to disconnect now ...", EAndroidLoggingLevel.Trace);

        if (_transport == null) {
            logInBg("[AFLD.TDISC.020] Transport is null so nothing to disconnect from", EAndroidLoggingLevel.Trace);
            return true;
        }

        try {
            _transport.release();
        } catch (Exception ex) {
            logInBg("[AFLD.TD.030] Failed to disconnect from the transport:\n\n" + ex, EAndroidLoggingLevel.Error);
            return false;
        }

        return true;
    }

    public void nativeDispose()
    {
        logInBg("[AFLD.ND.010] Disposing the native-firmware-list-downloader", EAndroidLoggingLevel.Trace);

        tryInvalidateCachedInfrastructure(); //  doesnt throw
        tryShutdownBackgroundExecutor(); //      doesnt throw
    }

    @SuppressWarnings("UnusedReturnValue")
    private boolean tryShutdownBackgroundExecutor()
    {
        logInBg("[AFLD.TSBE.010] Shutting down the background-executor ...", EAndroidLoggingLevel.Trace);

        try
        {
            _backgroundExecutor.shutdown();
            return true;
        }
        catch (final Exception ex)
        {
            logInBg("[AFLD.TBE.010] [SUPPRESSED] Error while shutting down background executor:\n\n" + ex, EAndroidLoggingLevel.Warning);
            return false;
        }
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize) {
        if (_transport == null) {
            logInBg("[AFLD.ETIIEO.000] Transport is null - instantiating it now", EAndroidLoggingLevel.Warning);

            _transport = new McuMgrBleTransport(_context, _bluetoothDevice);
        }

        if (initialMtuSize > 0) {
            _transport.setInitialMtu(initialMtuSize);
            logInBg("[AFLD.ETIIEO.010] Initial-MTU-size set explicitly to '" + initialMtuSize + "'", EAndroidLoggingLevel.Info);
        } else {
            logInBg("[AFLD.ETIIEO.020] Initial-MTU-size left to its nordic-default-value which is probably 498", EAndroidLoggingLevel.Info);
        }
    }

    private EAndroidFirmwareListDownloaderVerdict ensureImageManagerIsInitializedExactlyOnce() {
        if (_imageManager != null) //already initialized
            return EAndroidFirmwareListDownloaderVerdict.SUCCESS;

        logInBg("[AFLD.EFMIIEO.010] (Re)Initializing image-manager", EAndroidLoggingLevel.Trace);

        try {
            _imageManager = new ImageManager(_transport); //order
        } catch (final Exception ex) {
            return EAndroidFirmwareListDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidFirmwareListDownloaderVerdict.SUCCESS;
    }

    @SuppressWarnings("UnusedReturnValue")
    private boolean tryDisposeTransport() {
        if (_transport == null)
            return true; // already disposed

        boolean success = true;
        try {
            _transport.release();
        } catch (final Exception ex) // suppress
        {
            success = false;
            logInBg("[AFLD.TDT.010] Failed to release the transport:\n\n" + ex, EAndroidLoggingLevel.Error);
        }

        _transport = null;
        return success;
    }

    @SuppressWarnings("SameParameterValue")
    private void setLoggingEnabledOnTransport(final boolean enabled) {
        if (_transport == null)
            return;

        _transport.setLoggingEnabled(enabled);
    }

    private void fireAndForgetInTheBg(Runnable func) {
        if (func == null)
            return;

        _backgroundExecutor.execute(() -> {
            try {
                func.run();
            } catch (Exception ignored) {
                // ignored
            }
        });
    }

    private final String DefaultLogCategory = "FirmwareListDownloader";

    private void logInBg(final String message, final EAndroidLoggingLevel level) {
        if (level.ordinal() < _minimumNativeLogLevel.ordinal())
            return;

        fireAndForgetInTheBg(() -> logMessageAdvertisement(message, DefaultLogCategory, level.toString()));
    }

    @Contract(pure = true)
    public void logMessageAdvertisement(final String message, final String category, final String level) //wrapper method
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

}
