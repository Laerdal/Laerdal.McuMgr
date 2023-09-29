package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrCallback;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.DefaultManager;
import io.runtime.mcumgr.response.McuMgrResponse;
import org.jetbrains.annotations.NotNull;

@SuppressWarnings("unused")
public class AndroidDeviceResetter {

    private final DefaultManager _manager;

    /**
     * Constructs a firmware installer for a specific android-context and bluetooth-device.
     *
     * @param context         the android-context of the calling environment
     * @param bluetoothDevice the device to perform the firmware-install on
     */
    public AndroidDeviceResetter(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice) {
        final McuMgrTransport transport = new McuMgrBleTransport(context, bluetoothDevice);

        _manager = new DefaultManager(transport);
    }

    public void beginReset() {
        setState(EAndroidDeviceResetterState.RESETTING);

        _manager.reset(new McuMgrCallback<McuMgrResponse>() {

            @Override
            public void onResponse(@NotNull final McuMgrResponse response) {
                if (!response.isSuccess()) { // check for an error return code
                    fatalErrorOccurredAdvertisement("Reset failed (error-code '" + response.getReturnCode().toString() + "')");

                    setState(EAndroidDeviceResetterState.FAILED);
                    return;
                }

                setState(EAndroidDeviceResetterState.COMPLETE);
            }

            @Override
            public void onError(@NotNull final McuMgrException error) {
                fatalErrorOccurredAdvertisement("Reset failed '" + error.getMessage() + "'");

                setState(EAndroidDeviceResetterState.FAILED);
            }

        });
    }

    public void disconnect()
    {
        _manager.getTransporter().release();
    }

    public EAndroidDeviceResetterState getState() {
        return _currentState;
    }

    private EAndroidDeviceResetterState _currentState = EAndroidDeviceResetterState.NONE;
    private void setState(EAndroidDeviceResetterState newState) {
        final EAndroidDeviceResetterState oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(oldState, newState); //order
    }

    protected void onCleared() {
        // _manager.setFirmwareUpgradeCallback(null);
    }

    private String _lastFatalErrorMessage;

    public String getLastFatalErrorMessage() {
        return _lastFatalErrorMessage;
    }

    public void fatalErrorOccurredAdvertisement(String errorMessage) {
        _lastFatalErrorMessage = errorMessage; //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void logMessageAdvertisement(String message, String category, String level) {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void stateChangedAdvertisement(EAndroidDeviceResetterState oldState, EAndroidDeviceResetterState currentState) {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

}