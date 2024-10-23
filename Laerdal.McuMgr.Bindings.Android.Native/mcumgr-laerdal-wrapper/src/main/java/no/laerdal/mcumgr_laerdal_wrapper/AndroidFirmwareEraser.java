package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrCallback;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.ImageManager;
import io.runtime.mcumgr.response.img.McuMgrImageResponse;
import io.runtime.mcumgr.response.img.McuMgrImageStateResponse;
import org.jetbrains.annotations.Contract;

@SuppressWarnings("unused")
public class AndroidFirmwareEraser {

    private ImageManager _imageManager;
    private final McuMgrBleTransport _transport;

    /**
     * Constructs a firmware installer for a specific android-context and bluetooth-device.
     *
     * @param context         the android-context of the calling environment
     * @param bluetoothDevice the device to perform the firmware-install on
     */
    public AndroidFirmwareEraser(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice) {
        _transport = new McuMgrBleTransport(context, bluetoothDevice);
    }

    public void beginErasure(final int imageIndex) {
        busyStateChangedAdvertisement(true);

        setState(EAndroidFirmwareEraserState.ERASING);

        _imageManager = new ImageManager(_transport);
        _imageManager.erase(imageIndex, new McuMgrCallback<McuMgrImageResponse>() {
            @Override
            public void onResponse(@NonNull final McuMgrImageResponse response) {
                if (!response.isSuccess()) { // check for an error return code
                    fatalErrorOccurredAdvertisement("Erasure failed (error-code '" + response.getReturnCode().toString() + "')");

                    setState(EAndroidFirmwareEraserState.FAILED);
                    return;
                }

                readImageErasure();

                setState(EAndroidFirmwareEraserState.COMPLETE);
            }

            @Override
            public void onError(@NonNull final McuMgrException error) {
                fatalErrorOccurredAdvertisement("Erasure failed '" + error.getMessage() + "'");

                busyStateChangedAdvertisement(false);

                setState(EAndroidFirmwareEraserState.IDLE);
            }
        });
    }

    public void disconnect() {
        if (_imageManager == null)
            return;

        final McuMgrTransport mcuMgrTransporter = _imageManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        mcuMgrTransporter.release();
    }

    private EAndroidFirmwareEraserState _currentState = EAndroidFirmwareEraserState.NONE;
    private void setState(EAndroidFirmwareEraserState newState) {
        final EAndroidFirmwareEraserState oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(oldState, newState); //order
    }

    @Contract(pure = true)
    public void stateChangedAdvertisement(EAndroidFirmwareEraserState oldState, EAndroidFirmwareEraserState currentState) {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    private String _lastFatalErrorMessage;

    @Contract(pure = true)
    public String getLastFatalErrorMessage() {
        return _lastFatalErrorMessage;
    }

    public void fatalErrorOccurredAdvertisement(final String errorMessage) {
        _lastFatalErrorMessage = errorMessage; //this method is meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void logMessageAdvertisement(final String message, final String category, final String level) {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void busyStateChangedAdvertisement(final boolean busyNotIdle) {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    private void readImageErasure() {
        busyStateChangedAdvertisement(true);

        _imageManager.list(new McuMgrCallback<McuMgrImageStateResponse>() {

            @Override
            public void onResponse(@NonNull final McuMgrImageStateResponse response) {
                // postReady(response);
                busyStateChangedAdvertisement(false);
            }

            @Override
            public void onError(@NonNull final McuMgrException error) {
                fatalErrorOccurredAdvertisement(error.getMessage());
                busyStateChangedAdvertisement(false);
            }
        });
    }

}
