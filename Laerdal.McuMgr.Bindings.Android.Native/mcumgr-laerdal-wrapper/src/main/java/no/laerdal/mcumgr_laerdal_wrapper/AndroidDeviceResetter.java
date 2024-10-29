package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrCallback;
import io.runtime.mcumgr.McuMgrErrorCode;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.DefaultManager;
import io.runtime.mcumgr.response.HasReturnCode;
import io.runtime.mcumgr.response.dflt.McuMgrOsResponse;
import org.jetbrains.annotations.Contract;
import org.jetbrains.annotations.NotNull;

@SuppressWarnings("unused")
public class AndroidDeviceResetter
{

    private DefaultManager _manager;
    private final McuMgrTransport _transport;

    /**
     * Constructs a firmware installer for a specific android-context and bluetooth-device.
     *
     * @param context         the android-context of the calling environment
     * @param bluetoothDevice the device to perform the firmware-install on
     */
    public AndroidDeviceResetter(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _transport = new McuMgrBleTransport(context, bluetoothDevice);
    }

    public void beginReset()
    {
        try
        {
            _manager = new DefaultManager(_transport);
        }
        catch (final Exception ex)
        {
            setState(EAndroidDeviceResetterState.FAILED);
            onError("Failed to create manager: '" + ex.getMessage() + "'", ex);
            return;
        }

        setState(EAndroidDeviceResetterState.RESETTING);

        AndroidDeviceResetter self = this;

        _manager.reset(new McuMgrCallback<McuMgrOsResponse>()
        {

            @Override
            public void onResponse(@NotNull final McuMgrOsResponse response)
            {
                if (!response.isSuccess())
                { // check for an error return code
                    self.onError("Reset failed (error-code '" + response.getReturnCode().toString() + "')", response.getReturnCode(), response.getGroupReturnCode());

                    setState(EAndroidDeviceResetterState.FAILED);
                    return;
                }

                setState(EAndroidDeviceResetterState.COMPLETE);
            }

            @Override
            public void onError(@NotNull final McuMgrException exception)
            {
                self.onError("Reset failed '" + exception.getMessage() + "'", exception);

                setState(EAndroidDeviceResetterState.FAILED);
            }

        });
    }

    public void disconnect()
    {
        if (_manager == null)
            return;

        final McuMgrTransport mcuMgrTransporter = _manager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        mcuMgrTransporter.release();
    }

    public EAndroidDeviceResetterState getState()
    {
        return _currentState;
    }

    private EAndroidDeviceResetterState _currentState = EAndroidDeviceResetterState.NONE;

    private void setState(final EAndroidDeviceResetterState newState)
    {
        final EAndroidDeviceResetterState oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(oldState, newState); //order
    }

    protected void onCleared()
    {
        // _manager.setFirmwareUpgradeCallback(null);
    }

    private String _lastFatalErrorMessage;

    @Contract(pure = true)
    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    private void onError(final String errorMessage, final Exception exception)
    {
        onErrorImpl(errorMessage, McuMgrExceptionHelpers.DeduceGlobalErrorCodeFromException(exception));
    }

    private void onError(final String errorMessage, final McuMgrErrorCode exceptionCodeSpecs, final HasReturnCode.GroupReturnCode groupReturnCodeSpecs)
    {
        onErrorImpl(errorMessage, McuMgrExceptionHelpers.DeduceGlobalErrorCodeFromException(exceptionCodeSpecs, groupReturnCodeSpecs));
    }

    private void onErrorImpl(final String errorMessage, final int globalErrorCode)
    {
        setState(EAndroidDeviceResetterState.FAILED);

        fatalErrorOccurredAdvertisement(errorMessage, globalErrorCode);
    }

    public void fatalErrorOccurredAdvertisement(final String errorMessage, final int globalErrorCode)
    { //this method is meant to be overridden by csharp binding libraries to intercept updates
        _lastFatalErrorMessage = errorMessage;
    }

    @Contract(pure = true)
    public void logMessageAdvertisement(final String message, final String category, final String level)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void stateChangedAdvertisement(final EAndroidDeviceResetterState oldState, final EAndroidDeviceResetterState currentState)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

}
