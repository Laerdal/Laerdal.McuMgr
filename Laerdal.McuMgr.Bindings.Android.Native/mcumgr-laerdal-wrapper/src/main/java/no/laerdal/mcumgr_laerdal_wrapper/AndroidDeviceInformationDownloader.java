package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;

import androidx.annotation.NonNull;

import org.jetbrains.annotations.Contract;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.managers.ImageManager;
import io.runtime.mcumgr.response.img.McuMgrImageStateResponse;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;

@SuppressWarnings({"unused", "DuplicatedCode"})
public class AndroidDeviceInformationDownloader {
    private EAndroidLoggingLevel _minimumNativeLogLevel = EAndroidLoggingLevel.Error;

    private Context _context;
    private BluetoothDevice _bluetoothDevice;

    private ImageManager _imageManager;
    private McuMgrBleTransport _transport;

    private final ExecutorService _backgroundExecutor = Executors.newCachedThreadPool();

    public AndroidDeviceInformationDownloader()
    {
    }

    public AndroidDeviceInformationDownloader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice) {
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
            logInBg("[AFD.TSBD.020] Failed to invalidate the cached-transport instance", EAndroidLoggingLevel.Error);
            return false;
        }

        _bluetoothDevice = bluetoothDevice; //order

        logInBg("[AFD.TSBD.030] Successfully set the android-bluetooth-device to the given value", EAndroidLoggingLevel.Trace);

        return true;
    }

    public boolean tryInvalidateCachedInfrastructure() {
        return tryDisposeTransport(); //         order
    }

    public String beginDownload(
            final int initialMtuSize,
            final int minimumNativeLogLevelNumeric
    ) {
        if (_context == null) {
            return EAndroidDeviceInformationDownloaderVerdict.FAILED__INVALID_SETTINGS.name();
        }

        if (_bluetoothDevice == null) {
            return EAndroidDeviceInformationDownloaderVerdict.FAILED__INVALID_SETTINGS.name();
        }

        _minimumNativeLogLevel = McuMgrLogLevelHelpers.translateLogLevel(minimumNativeLogLevelNumeric);

        try {
            ensureTransportIsInitializedExactlyOnce(initialMtuSize); //order
            setLoggingEnabledOnTransport(false); //order

            final EAndroidDeviceInformationDownloaderVerdict verdict = ensureImageManagerIsInitializedExactlyOnce(); //order
            if (verdict != EAndroidDeviceInformationDownloaderVerdict.SUCCESS)
                return verdict.name();

            tryEnsureConnectionPriorityOnTransport(); //order

            return parseInformation(_imageManager.list());
        } catch (final Exception ex) {
            return EAndroidDeviceInformationDownloaderVerdict.FAILED__INVALID_DATA.name();
        }
    }

    @NonNull
    private static String parseInformation(McuMgrImageStateResponse response) {
        StringBuilder builder = new StringBuilder();
        for (McuMgrImageStateResponse.ImageSlot image : response.images) {
            builder.append("\n-------------------------------------------");
            builder.append("\nVersion: ").append(image.version);
            builder.append("\nSlot #").append(image.slot);
            builder.append("\nActive: ").append(image.active);
            builder.append("\nBootable: ").append(image.bootable);
            builder.append("\nCompressed: ").append(image.compressed);
            builder.append("\nConfirmed: ").append(image.confirmed);
            builder.append("\nImage number: ").append(image.image);
            builder.append("\nPermanent: ").append(image.permanent);
        }
        return builder.toString();
    }

    @SuppressWarnings("UnusedReturnValue")
    public boolean tryDisconnect() {
        logInBg("[AFD.TDISC.010] Will try to disconnect now ...", EAndroidLoggingLevel.Trace);

        if (_transport == null) {
            logInBg("[AFD.TDISC.020] Transport is null so nothing to disconnect from", EAndroidLoggingLevel.Trace);
            return true;
        }

        try {
            _transport.release();
        } catch (Exception ex) {
            logInBg("[AFD.TD.010] Failed to disconnect from the transport:\n\n" + ex, EAndroidLoggingLevel.Error);
            return false;
        }

        return true;
    }

    @SuppressWarnings("UnusedReturnValue")
    private boolean tryShutdownBackgroundExecutor() {
        logInBg("[AFD.TSBE.010] Shutting down the background-executor ...", EAndroidLoggingLevel.Trace);

        try {
            _backgroundExecutor.shutdown();
            return true;
        } catch (final Exception ex) {
            logInBg("[AFD.TBE.010] [SUPPRESSED] Error while shutting down background executor:\n\n" + ex, EAndroidLoggingLevel.Warning);
            return false;
        }
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize) {
        if (_transport == null) {
            logInBg("[AFD.ETIIEO.000] Transport is null - instantiating it now", EAndroidLoggingLevel.Warning);

            _transport = new McuMgrBleTransport(_context, _bluetoothDevice);
        }

        if (initialMtuSize > 0) {
            _transport.setInitialMtu(initialMtuSize);
            logInBg("[AFD.ETIIEO.010] Initial-MTU-size set explicitly to '" + initialMtuSize + "'", EAndroidLoggingLevel.Info);
        } else {
            logInBg("[AFD.ETIIEO.020] Initial-MTU-size left to its nordic-default-value which is probably 498", EAndroidLoggingLevel.Info);
        }
    }

    private EAndroidDeviceInformationDownloaderVerdict ensureImageManagerIsInitializedExactlyOnce() {
        if (_imageManager != null) //already initialized
            return EAndroidDeviceInformationDownloaderVerdict.SUCCESS;

        logInBg("[AFD.EFMIIEO.010] (Re)Initializing image-manager", EAndroidLoggingLevel.Trace);

        try {
            _imageManager = new ImageManager(_transport); //order
        } catch (final Exception ex) {
            return EAndroidDeviceInformationDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidDeviceInformationDownloaderVerdict.SUCCESS;
    }

    private void tryEnsureConnectionPriorityOnTransport() {
        _transport.requestConnPriority(ConnectionPriorityRequest.CONNECTION_PRIORITY_HIGH);
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
            logInBg("[AFD.TDT.010] Failed to release the transport:\n\n" + ex, EAndroidLoggingLevel.Error);
        }

        _transport = null;
        return success;
    }

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

    private final String DefaultLogCategory = "FileDownloader";

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
