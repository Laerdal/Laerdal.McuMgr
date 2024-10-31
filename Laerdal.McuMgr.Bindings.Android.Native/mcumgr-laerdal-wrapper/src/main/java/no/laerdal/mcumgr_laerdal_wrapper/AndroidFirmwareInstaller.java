package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.SystemClock;
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

@SuppressWarnings("unused")
public class AndroidFirmwareInstaller
{
    private Handler _handler;

    @SuppressWarnings("FieldCanBeLocal")
    private HandlerThread _handlerThread;
    private FirmwareUpgradeManager _manager;
    private final McuMgrTransport _transport;

    private int _imageSize;
    private int _bytesSent;
    private int _lastProgress;
    private int _bytesSentSinceUploadStated;
    private long _uploadStartTimestamp;

    private static final int NOT_STARTED = -1; // a value indicating that the upload has not been started before
    private static final long REFRESH_RATE = 100L; // ms how often the throughput data should be sent to the graph

    /**
     * Constructs a firmware installer for a specific android-context and bluetooth-device.
     *
     * @param context         the android-context of the calling environment
     * @param bluetoothDevice the device to perform the firmware-install on
     */
    public AndroidFirmwareInstaller(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _transport = new McuMgrBleTransport(context, bluetoothDevice);
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
        if (_currentState != EAndroidFirmwareInstallationState.NONE //if an installation is already in progress we bail out
                && _currentState != EAndroidFirmwareInstallationState.ERROR
                && _currentState != EAndroidFirmwareInstallationState.COMPLETE
                && _currentState != EAndroidFirmwareInstallationState.CANCELLED)
        {
            onError(EAndroidFirmwareInstallerFatalErrorType.FAILED__INSTALLATION_ALREADY_IN_PROGRESS, "[AFI.BI.000] Another firmware installation is already in progress");

            return EAndroidFirmwareInstallationVerdict.FAILED__INSTALLATION_ALREADY_IN_PROGRESS;
        }

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
                onError(EAndroidFirmwareInstallerFatalErrorType.INVALID_FIRMWARE, ex2.getMessage(), ex2);

                return EAndroidFirmwareInstallationVerdict.FAILED__INVALID_DATA_FILE;
            }
        }

        _manager = new FirmwareUpgradeManager(_transport);
        _manager.setFirmwareUpgradeCallback(new FirmwareInstallCallbackProxy());

        _handlerThread = new HandlerThread("AndroidFirmwareInstaller.HandlerThread"); //todo   peer review whether this is the best way to go    maybe we should be getting this from the call environment?
        _handlerThread.start();

        _handler = new Handler(_handlerThread.getLooper());

        if (estimatedSwapTimeInMilliseconds >= 0 && estimatedSwapTimeInMilliseconds <= 1000)
        { //it is better to just warn the calling environment instead of erroring out
            logMessageAdvertisement(
                    "Estimated swap-time of '" + estimatedSwapTimeInMilliseconds + "' milliseconds seems suspiciously low - did you mean to say '" + (estimatedSwapTimeInMilliseconds * 1000) + "' milliseconds?",
                    "FirmwareInstaller",
                    "WARN"
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
            onError(EAndroidFirmwareInstallerFatalErrorType.INVALID_SETTINGS, ex.getMessage(), ex);

            return EAndroidFirmwareInstallationVerdict.FAILED__INVALID_SETTINGS;
        }

        try
        {
            setState(EAndroidFirmwareInstallationState.IDLE);

            _manager.start(images, settings);
        }
        catch (final Exception ex)
        {
            onError(EAndroidFirmwareInstallerFatalErrorType.DEPLOYMENT_FAILED, ex.getMessage(), ex);

            return EAndroidFirmwareInstallationVerdict.FAILED__DEPLOYMENT_ERROR;
        }

        return EAndroidFirmwareInstallationVerdict.SUCCESS;
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
        if (_manager == null)
            return;

        final McuMgrTransport mcuMgrTransporter = _manager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        mcuMgrTransporter.release();
    }

    private EAndroidFirmwareInstallationState _currentState = EAndroidFirmwareInstallationState.NONE;

    private void setState(final EAndroidFirmwareInstallationState newState)
    {
        final EAndroidFirmwareInstallationState oldState = _currentState; //order

        _currentState = newState; //order

        if (oldState == EAndroidFirmwareInstallationState.UPLOADING && newState == EAndroidFirmwareInstallationState.TESTING) //00   order
        {
            firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0);
        }

        stateChangedAdvertisement(oldState, newState); //order

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
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
        fatalErrorOccurredAdvertisement( //                                           order
                currentStateSnapshot,
                fatalErrorType,
                errorMessage,
                McuMgrExceptionHelpers.DeduceGlobalErrorCodeFromException(ex)
        );

        //00   we want to let the calling environment know in which exact state the fatal error happened in
    }

    //this method is meant to be overridden by csharp binding libraries to intercept updates
    public void fatalErrorOccurredAdvertisement(final EAndroidFirmwareInstallationState state, final EAndroidFirmwareInstallerFatalErrorType fatalErrorType, final String errorMessage, final int globalErrorCode)
    {
        _lastFatalErrorMessage = errorMessage;
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
    public void firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(final int progressPercentage, final float averageThroughput)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void pause()
    {
        if (!_manager.isInProgress())
            return;

        _handler.removeCallbacks(_graphUpdater);
        setState(EAndroidFirmwareInstallationState.PAUSED);
        _manager.pause();

        // Timber.i("Upload paused"); //todo  logging
        setLoggingEnabled(true);
        busyStateChangedAdvertisement(false);
    }

    public void resume()
    {
        if (!_manager.isPaused())
            return;

        busyStateChangedAdvertisement(true);
        setState(EAndroidFirmwareInstallationState.UPLOADING);

        // Timber.i("Upload resumed");
        _bytesSentSinceUploadStated = NOT_STARTED;
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
            busyStateChangedAdvertisement(true);
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
                    //Timber.i("Uploading firmware..."); //todo    logging
                    _bytesSentSinceUploadStated = NOT_STARTED;
                    setState(EAndroidFirmwareInstallationState.UPLOADING);
                    break;
                case TEST:
                    _handler.removeCallbacks(_graphUpdater);
                    setState(EAndroidFirmwareInstallationState.TESTING);
                    break;
                case RESET:
                    setState(EAndroidFirmwareInstallationState.RESETTING);
                    break;
                case CONFIRM:
                    _handler.removeCallbacks(_graphUpdater);
                    setState(EAndroidFirmwareInstallationState.CONFIRMING);
                    break;

                default:
                    break; // setState(.idle);  //dont
            }
        }

        @Override
        public void onUpgradeCompleted()
        {
            _handler.removeCallbacks(_graphUpdater);

            setState(EAndroidFirmwareInstallationState.COMPLETE);
            // Timber.i("Install complete");
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onUpgradeFailed(final FirmwareUpgradeManager.State state, final McuMgrException ex)
        {
            _handler.removeCallbacks(_graphUpdater);

            EAndroidFirmwareInstallerFatalErrorType fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.GENERIC;
            if (state == State.UPLOAD)
            {
                fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.FIRMWARE_UPLOADING_ERRORED_OUT;
            }
            else if (state == State.CONFIRM && ex instanceof McuMgrTimeoutException)
            {
                fatalErrorType = EAndroidFirmwareInstallerFatalErrorType.FIRMWARE_IMAGE_SWAP_TIMEOUT;
            }

            onError(fatalErrorType, ex.getMessage(), ex);
            setLoggingEnabled(true);
            // Timber.e(error, "Install failed");
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onUpgradeCanceled(final FirmwareUpgradeManager.State state)
        {
            _handler.removeCallbacks(_graphUpdater);

            setState(EAndroidFirmwareInstallationState.CANCELLED);
            cancelledAdvertisement();
            // Timber.w("Install cancelled");
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onUploadProgressChanged(final int bytesSent, final int imageSize, final long timestamp)
        {
            _imageSize = imageSize;
            _bytesSent = bytesSent;

            final long uptimeMillis = SystemClock.uptimeMillis();
            if (_bytesSentSinceUploadStated == NOT_STARTED) //00
            {
                _lastProgress = NOT_STARTED;

                _uploadStartTimestamp = uptimeMillis; //20
                _bytesSentSinceUploadStated = bytesSent;

                _handler.removeCallbacks(_graphUpdater);
                _handler.postAtTime(_graphUpdater, uptimeMillis + REFRESH_RATE); //30
            }

            final boolean uploadComplete = bytesSent == imageSize;
            if (!uploadComplete) //40
                return;

            //Timber.i("Image (%d bytes) sent in %d ms (avg speed: %f kB/s)", imageSize - bytesSentSinceUploadStated, uptimeMillis - uploadStartTimestamp, (float) (imageSize - bytesSentSinceUploadStated) / (float) (uptimeMillis - uploadStartTimestamp));

            _graphUpdater.run(); //50
            _bytesSentSinceUploadStated = NOT_STARTED; //60

            //00 check if this is the first time this method is called since:
            //
            //    - the start of an upload
            //    - after resume
            //
            //20 To calculate the throughput it is necessary to store the initial timestamp and the number of bytes sent so far
            //   Mind, that the upload may be resumed from any point, not necessarily from the beginning
            //
            //30 Begin updating the graph
            //
            //40 we need to ensure that the upload has completed before we reset the counter
            //
            //50 we explicitly invoke the graphupdater one last time to force it into completing the graph for good
            //
            //60 reset the initial bytes counter so if there is a next image uploaded afterward it will start the throughput calculations again
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

    private final Runnable _graphUpdater = new Runnable()
    {
        @Override
        public void run()
        {
            if (_manager.getState() != FirmwareUpgradeManager.State.UPLOAD || _manager.isPaused())
                return;

            final long timestamp = SystemClock.uptimeMillis();
            final int progressPercentage = (int) (_bytesSent * 100.f /* % */ / _imageSize); //0
            if (_lastProgress != progressPercentage)
            {
                _lastProgress = progressPercentage;

                final float timeSinceUploadStarted = timestamp - _uploadStartTimestamp;
                final float bytesSentSinceUploadStarted = _bytesSent - _bytesSentSinceUploadStated; //1

                final float averageThroughput = bytesSentSinceUploadStarted / timeSinceUploadStarted; //2

                firmwareUploadProgressPercentageAndDataThroughputChangedAdvertisement(progressPercentage, averageThroughput);
            }

            if (_manager.getState() == FirmwareUpgradeManager.State.UPLOAD && !_manager.isPaused())
            {
                _handler.postAtTime(this, timestamp + REFRESH_RATE);
            }

            //0 calculate the current upload progress
            //
            //1 calculate the average throughout   this is done by diving number of bytes sent since upload has been started (or resumed) by the time
            //  since that moment   the minimum time of MIN_INTERVAL ms prevents from graph peaks that may happen under certain conditions
            //
            //2 bytes / ms = KB/s
        }
    };
}
