package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.dfu.FirmwareUpgradeCallback;
import io.runtime.mcumgr.dfu.FirmwareUpgradeController;
import io.runtime.mcumgr.dfu.mcuboot.FirmwareUpgradeManager;
import io.runtime.mcumgr.dfu.mcuboot.FirmwareUpgradeManager.Settings;
import io.runtime.mcumgr.dfu.mcuboot.FirmwareUpgradeManager.Settings.Builder;
import io.runtime.mcumgr.dfu.mcuboot.FirmwareUpgradeManager.State;
import io.runtime.mcumgr.dfu.mcuboot.model.ImageSet;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.exception.McuMgrTimeoutException;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;
import org.jetbrains.annotations.Contract;
import org.jetbrains.annotations.NotNull;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

@SuppressWarnings("unused")
public class AndroidFirmwareInstaller
{
    @SuppressWarnings("FieldCanBeLocal")
    private final Context _context;
    @SuppressWarnings("FieldCanBeLocal")
    private final BluetoothDevice _bluetoothDevice;

    private McuMgrTransport _transport;
    private FirmwareUpgradeManager _manager;

    private long _uploadStartTimestampInMs;

    private int _lastBytesSent;
    private long _lastBytesSentTimestampInMs;

    private Boolean _currentBusyState = false;
    private EAndroidFirmwareInstallationState _currentState = EAndroidFirmwareInstallationState.NONE;

    private final ExecutorService _backgroundExecutor = Executors.newCachedThreadPool();

    /**
     * Constructs a firmware installer for a specific android-context and bluetooth-device.
     *
     * @param context         the android-context of the calling environment
     * @param bluetoothDevice the device to perform the firmware-install on
     */
    public AndroidFirmwareInstaller(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _context = context;
        _bluetoothDevice = bluetoothDevice;
    }

    /**
     * Initiates a firmware installation asynchronously. The progress is advertised through the callbacks provided by this class.
     * Setup interceptors for them to get informed about the status of the firmware-installation.
     *
     * @param data                            the firmware bytes to install - can also be a zipped byte stream
     * @param mode                            the mode of the installation - typically you want to set this to TEST_AND_CONFIRM in production environments
     * @param initialMtuSize                  sets the initial MTU for the connection that the McuMgr BLE-transport sets up for the firmware installation that will follow.
     *                                        Note that if less than 0 it gets ignored and if it doesn't fall within the range [23, 517] it will cause a hard error.
     * @param eraseSettings                   specifies whether the previous settings should be erased on the target-device
     * @param estimatedSwapTimeInMilliseconds specifies the amount of time to wait before probing the device to see if the firmware that got installed managed to reboot the device successfully - if negative the setting gets ignored
     * @param windowCapacity                  specifies the windows-capacity for the data transfers of the BLE connection - if zero or negative the value provided gets ignored and will be set to 1 by default
     * @param memoryAlignment                 specifies the memory-alignment to use for the data transfers of the BLE connection - if zero or negative the value provided gets ignored and will be set to 1 by default
     * @return a verdict indicating whether the firmware installation was started successfully or not
     */
    public EAndroidFirmwareInstallationVerdict beginInstallation(
            @NonNull final byte[] data,
            @NonNull final EAndroidFirmwareInstallationMode mode,
            final int initialMtuSize,
            final boolean eraseSettings,
            final int estimatedSwapTimeInMilliseconds,
            final int windowCapacity,
            final int memoryAlignment
    )
    {
        if (!IsCold()) //if an installation is already in progress we bail out
        {
            onError(EAndroidFirmwareInstallerFatalErrorType.INSTALLATION_ALREADY_IN_PROGRESS, "[AFI.BI.000] Another firmware installation is already in progress");

            return EAndroidFirmwareInstallationVerdict.FAILED__INSTALLATION_ALREADY_IN_PROGRESS;
        }

        resetInstallationTidbits();

        ImageSet images = new ImageSet();
        try
        {
            images.add(data); //the method healthchecks the bytes itself internally so we dont have to do it ourselves here manually
        }
        catch (final Exception ex)
        {
            try
            {
                images.add(new ZipPackage(data).getBinaries()); //the method healthchecks the bytes itself internally so we dont have to do it ourselves here manually
            }
            catch (final Exception ex2)
            {
                onError(EAndroidFirmwareInstallerFatalErrorType.GIVEN_FIRMWARE_DATA_UNHEALTHY, "[AFI.BI.010] Failed to extract firmware-images" + ex2, ex2);

                return EAndroidFirmwareInstallationVerdict.FAILED__GIVEN_FIRMWARE_UNHEALTHY;
            }
        }

        _transport = new McuMgrBleTransport(_context, _bluetoothDevice);
        _manager = new FirmwareUpgradeManager(_transport);
        _manager.setFirmwareUpgradeCallback(new FirmwareInstallCallbackProxy());

        if (estimatedSwapTimeInMilliseconds >= 0 && estimatedSwapTimeInMilliseconds <= 1000)
        { //it is better to just warn the calling environment instead of erroring out
            emitLogEntry(
                    "Estimated swap-time of '" + estimatedSwapTimeInMilliseconds + "' milliseconds seems suspiciously low - did you mean to say '" + (estimatedSwapTimeInMilliseconds * 1000) + "' milliseconds?",
                    "firmware-installer",
                    EAndroidLoggingLevel.Warning
            );
        }

        @NotNull Settings settings;
        try
        {
            configureConnectionSettings(initialMtuSize);

            settings = digestFirmwareInstallationManagerSettings(
                    mode,
                    eraseSettings,
                    estimatedSwapTimeInMilliseconds,
                    windowCapacity,
                    memoryAlignment
            );
        }
        catch (final Exception ex)
        {
            onError(EAndroidFirmwareInstallerFatalErrorType.INVALID_SETTINGS, "[AFI.BI.020] Failed to digest settings:\n\n" + ex, ex);

            return EAndroidFirmwareInstallationVerdict.FAILED__INVALID_SETTINGS;
        }

        try
        {
            setBusyState(false);
            setState(EAndroidFirmwareInstallationState.IDLE); //order
            _manager.start(images, settings); //order
        }
        catch (final Exception ex)
        {
            onError(EAndroidFirmwareInstallerFatalErrorType.INSTALLATION_INITIALIZATION_FAILED, "[AFI.BI.030] Failed to kick-start the installation:\n\n" + ex, ex);

            return EAndroidFirmwareInstallationVerdict.FAILED__INSTALLATION_INITIALIZATION_ERRORED_OUT;
        }

        return EAndroidFirmwareInstallationVerdict.SUCCESS;
    }

    private void fireAndForgetInTheBg(Runnable func)
    {
        if (func == null)
            return;

        _backgroundExecutor.execute(() -> {
            try
            {
                func.run();
            }
            catch (Exception ignored)
            {
                // ignored
            }
        });
    }

    private void resetInstallationTidbits()
    {
        _uploadStartTimestampInMs = 0;

        _lastBytesSent = 0;
        _lastBytesSentTimestampInMs = 0;

        setState(EAndroidFirmwareInstallationState.NONE);
        setBusyState(false);

        firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0, 0);
    }

    @Contract(pure = true)
    private boolean IsCold()
    {
        return _currentState == EAndroidFirmwareInstallationState.NONE
                || _currentState == EAndroidFirmwareInstallationState.ERROR
                || _currentState == EAndroidFirmwareInstallationState.COMPLETE
                || _currentState == EAndroidFirmwareInstallationState.CANCELLED;
    }

    private @NotNull Settings digestFirmwareInstallationManagerSettings(@NotNull EAndroidFirmwareInstallationMode mode, boolean eraseSettings, int estimatedSwapTimeInMilliseconds, int windowCapacity, int memoryAlignment)
    {
        Builder settingsBuilder = new FirmwareUpgradeManager.Settings.Builder();

        _manager.setMode(mode.getValueFirmwareUpgradeManagerMode()); //0

        if (estimatedSwapTimeInMilliseconds >= 0)
        {
            settingsBuilder.setEstimatedSwapTime(estimatedSwapTimeInMilliseconds); //1
        }

        if (windowCapacity >= 2)
        {
            settingsBuilder.setWindowCapacity(windowCapacity); //2
        }

        if (memoryAlignment >= 2)
        {
            settingsBuilder.setMemoryAlignment(memoryAlignment); //3
        }

        settingsBuilder.setEraseAppSettings(eraseSettings);

        return settingsBuilder.build();


        //0 set the installation mode
        //
        //1 rF52840 due to how the flash memory works requires ~20 sec to erase images
        //
        //2 Set the window capacity. Values > 1 enable a new implementation for uploading the images, which makes use of SMP pipelining feature.
        //
        //  The app will send this many packets immediately, without waiting for notification confirming each packet. This value should be lower than or
        //  equal to MCUMGR_BUF_COUNT
        //
        //  (https://github.com/zephyrproject-rtos/zephyr/blob/bd4ddec0c8c822bbdd420bd558b62c1d1a532c16/subsys/mgmt/mcumgr/Kconfig#L550)
        //
        //  Parameter in KConfig in NCS / Zephyr configuration and should also be supported on Mynewt devices.
        //
        //  Mind, that in Zephyr,  before https://github.com/zephyrproject-rtos/zephyr/pull/41959 was merged, the device required data to be sent
        //  with memory alignment. Otherwise, the device would ignore uneven bytes and reply with lower than expected offset causing multiple packets
        //  to be sent again dropping the speed instead of increasing it.
        //
        //3 Set the selected memory alignment. In the app this defaults to 4 to match Nordic devices, but can be modified in the UI.
    }

    public void disconnect()
    {
        if (_transport == null)
        {
            emitLogEntry("Transport is null - no need to disconnect", "firmware-installer", EAndroidLoggingLevel.Verbose);
            return;
        }

        try
        {
            _transport.release();
            _backgroundExecutor.shutdownNow();
            emitLogEntry("Connection closed!", "firmware-installer", EAndroidLoggingLevel.Info);
        }
        catch (Exception ex)
        {
            emitLogEntry("Failed to close transport connection: " + ex.getMessage(), "firmware-installer", EAndroidLoggingLevel.Error);
        }
    }

    private void setBusyState(final boolean newBusyState)
    {
        if (_currentBusyState == newBusyState)
            return;

        _currentBusyState = newBusyState;

        fireAndForgetInTheBg(() -> busyStateChangedAdvertisement(newBusyState));
    }

    private void setState(final EAndroidFirmwareInstallationState newState)
    {
        if (_currentState == newState)
            return; //no change

        final EAndroidFirmwareInstallationState oldState = _currentState; //order

        _currentState = newState; //order

        fireAndForgetInTheBg(() -> {
            if (oldState == EAndroidFirmwareInstallationState.UPLOADING && newState == EAndroidFirmwareInstallationState.TESTING) //00
            {
                firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0, 0); //order
            }

            stateChangedAdvertisement(oldState, newState); //order
        });

        //00  trivial hotfix to deal with the fact that the file-upload progress% doesnt fill up to 100%
    }

    protected void onCleared()
    {
        _manager.setFirmwareUpgradeCallback(null);
    }

    private String _lastFatalErrorMessage;

    @Contract(pure = true)
    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    public void onError(final EAndroidFirmwareInstallerFatalErrorType fatalErrorType, final String errorMessage)
    {
        onError(fatalErrorType, errorMessage, null);
    }

    public void onError(final EAndroidFirmwareInstallerFatalErrorType fatalErrorType, final String errorMessage, final Exception ex)
    {
        EAndroidFirmwareInstallationState currentStateSnapshot = _currentState; //00  order
        setState(EAndroidFirmwareInstallationState.ERROR); //                         order

        _lastFatalErrorMessage = errorMessage; //                                     order
        fireAndForgetInTheBg(() -> fatalErrorOccurredAdvertisement( //                order
                currentStateSnapshot,
                fatalErrorType,
                errorMessage,
                McuMgrExceptionHelpers.DeduceGlobalErrorCodeFromException(ex)
        ));

        //00   we want to let the calling environment know in which exact state the fatal error happened in
    }

    //this method is meant to be overridden by csharp binding libraries to intercept updates
    public void fatalErrorOccurredAdvertisement(final EAndroidFirmwareInstallationState state, final EAndroidFirmwareInstallerFatalErrorType fatalErrorType, final String errorMessage, final int globalErrorCode)
    {
    }

    //@Contract(pure = true) //dont
    private void emitLogEntry(final String message, final String category, final EAndroidLoggingLevel level)
    {
        fireAndForgetInTheBg(() -> logMessageAdvertisement(message, category, level.toString()));
    }

    @Contract(pure = true)
    public void logMessageAdvertisement(final String message, final String category, final String level)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void cancelledAdvertisement()
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void busyStateChangedAdvertisement(final boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void stateChangedAdvertisement(final EAndroidFirmwareInstallationState oldState, final EAndroidFirmwareInstallationState currentState)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(final int progressPercentage, final float currentThroughputInKBps, final float totalAverageThroughputInKBps)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void pause()
    {
        if (!_manager.isInProgress())
            return;

        setBusyState(false);
        setState(EAndroidFirmwareInstallationState.PAUSED);

        _manager.pause();
        setLoggingEnabled(true);
    }

    public void resume()
    {
        if (!_manager.isPaused())
            return;

        setBusyState(true);
        setState(EAndroidFirmwareInstallationState.UPLOADING);

        setLoggingEnabled(false);
        _manager.resume();
    }

    public void cancel()
    {
        setState(EAndroidFirmwareInstallationState.CANCELLING); //   order
        _manager.cancel(); //                                        order
    }

    //to future maintainers    in the csharp bindings this generates a phony warning that does not make sense   something about the
    //to future maintainers    symbol FirmwareInstallCallback not being found   but the generated nuget works just fine   go figure
    private final class FirmwareInstallCallbackProxy implements FirmwareUpgradeCallback<FirmwareUpgradeManager.State>
    {
        @Override
        public void onUpgradeStarted(final FirmwareUpgradeController controller)
        {
            setBusyState(true);
            setState(EAndroidFirmwareInstallationState.VALIDATING);
        }

        @Override
        public void onStateChanged(
                final FirmwareUpgradeManager.State prevState,
                final FirmwareUpgradeManager.State newState
        )
        {
            setLoggingEnabled(newState != State.UPLOAD);

            switch (newState)
            {
                case VALIDATE:
                    setState(EAndroidFirmwareInstallationState.VALIDATING);
                    break;
                case UPLOAD:
                    setState(EAndroidFirmwareInstallationState.UPLOADING);
                    break;
                case TEST:
                    setState(EAndroidFirmwareInstallationState.TESTING);
                    break;
                case RESET:
                    setState(EAndroidFirmwareInstallationState.RESETTING);
                    break;
                case CONFIRM:
                    setState(EAndroidFirmwareInstallationState.CONFIRMING);
                    break;

                default:
                    break; // setState(.idle);  //dont
            }
        }

        @Override
        public void onUpgradeCompleted()
        {
            setState(EAndroidFirmwareInstallationState.COMPLETE);
            setBusyState(false);

            setLoggingEnabled(true);
        }

        @Override
        public void onUpgradeFailed(final FirmwareUpgradeManager.State state, final McuMgrException ex)
        {
            EAndroidFirmwareInstallerFatalErrorType fatalErrorType = DeduceInstallationFailureType(state, ex);

            onError(fatalErrorType, "[AFI.OAF.010] Upgrade failed:\n\n" + ex, ex);
            setBusyState(false);

            setLoggingEnabled(true);
        }

        private EAndroidFirmwareInstallerFatalErrorType DeduceInstallationFailureType(final FirmwareUpgradeManager.State state, final McuMgrException ex)
        {
            EAndroidFirmwareInstallerFatalErrorType fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.GENERIC;

            switch (state)
            {
                case NONE: //impossible to happen   should default to GENERIC
                    break;

                case VALIDATE:
                    fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.FIRMWARE_STRICT_DATA_INTEGRITY_CHECKS_FAILED; //crc checks failed before the installation even commenced
                    break;

                case UPLOAD:
                    fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.FIRMWARE_UPLOADING_ERRORED_OUT; //todo  improve this heuristic once we figure out the exact type of exception we get in case of an upload error
                    break;

                case TEST:
                    fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.POST_INSTALLATION_DEVICE_HEALTHCHECK_TESTS_FAILED;
                    break;

                case RESET:
                    fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.POST_INSTALLATION_DEVICE_REBOOTING_FAILED;
                    break;

                case CONFIRM:
                    fatalErrorType = ex instanceof McuMgrTimeoutException
                                 ? EAndroidFirmwareInstallerFatalErrorType.FIRMWARE_FINISHING_IMAGE_SWAP_TIMEOUT
                                 : EAndroidFirmwareInstallerFatalErrorType.FIRMWARE_POST_INSTALLATION_CONFIRMATION_FAILED;
                    break;

                default:
                    break; //better not throw here
            }

            return fatalErrorType;
        }

        @Override
        public void onUpgradeCanceled(final FirmwareUpgradeManager.State state)
        {
            setState(EAndroidFirmwareInstallationState.CANCELLED);
            cancelledAdvertisement();

            setLoggingEnabled(true);
            setBusyState(false);
        }

        @Override
        public void onUploadProgressChanged(final int totalBytesSentSoFar, final int imageSize, final long timestamp)
        {
            if (imageSize == 0)
                return;

            fireAndForgetInTheBg(() -> {
                int lastProgress = (int) (totalBytesSentSoFar * 100.f /* % */ / imageSize);
                float currentThroughputInKBps = calculateCurrentThroughputInKBps(totalBytesSentSoFar, timestamp);
                float totalAverageThroughputInKBps = calculateTotalAverageThroughputInKBps(totalBytesSentSoFar, timestamp);

                firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement( //fire-and-forget in the bg to help performance a bit
                        lastProgress,
                        currentThroughputInKBps,
                        totalAverageThroughputInKBps
                );
            });
        }

        @SuppressWarnings("DuplicatedCode")
        private float calculateCurrentThroughputInKBps(final int totalBytesSentSoFar, final long timestampInMs) {
            if (_lastBytesSentTimestampInMs == 0) {
                _lastBytesSent = totalBytesSentSoFar;
                _lastBytesSentTimestampInMs = timestampInMs;
                return 0;
            }

            float intervalInSeconds = ((float)(timestampInMs - _lastBytesSentTimestampInMs)) / 1_000;
            if (intervalInSeconds == 0) { //almost impossible to happen but just in case
                _lastBytesSent = totalBytesSentSoFar;
                _lastBytesSentTimestampInMs = timestampInMs;
                return 0;
            }

            float currentThroughputInKBps = ((float) (totalBytesSentSoFar - _lastBytesSent)) / (intervalInSeconds * 1_024); //order

            _lastBytesSent = totalBytesSentSoFar; //order
            _lastBytesSentTimestampInMs = timestampInMs; //order

            return currentThroughputInKBps;
        }

        @SuppressWarnings("DuplicatedCode")
        private float calculateTotalAverageThroughputInKBps(final int totalBytesSentSoFar, final long timestampInMs) {
            if (_uploadStartTimestampInMs == 0) {
                _uploadStartTimestampInMs = timestampInMs;
                return 0;
            }

            float elapsedSecondSinceUploadStart = ((float)(timestampInMs - _uploadStartTimestampInMs)) / 1_000;
            if (elapsedSecondSinceUploadStart == 0) { //should be impossible but just in case
                return 0;
            }

            return (float)(totalBytesSentSoFar) / (elapsedSecondSinceUploadStart * 1_024);
        }
    }

    private void configureConnectionSettings(int initialMtuSize)
    {
        final McuMgrTransport transporter = _manager.getTransporter();
        if (!(transporter instanceof McuMgrBleTransport))
            return;

        final McuMgrBleTransport bleTransporter = (McuMgrBleTransport) transporter;

        if (initialMtuSize > 0)
        {
            bleTransporter.setInitialMtu(initialMtuSize);
        }

        bleTransporter.requestConnPriority(ConnectionPriorityRequest.CONNECTION_PRIORITY_HIGH);
    }

    private void setLoggingEnabled(final boolean enabled)
    {
        final McuMgrTransport transporter = _manager.getTransporter();
        if (!(transporter instanceof McuMgrBleTransport))
            return;

        final McuMgrBleTransport bleTransporter = (McuMgrBleTransport) transporter;
        bleTransporter.setLoggingEnabled(enabled);
    }
}
