package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.FsManager;
import io.runtime.mcumgr.transfer.FileUploader;
import io.runtime.mcumgr.transfer.TransferController;
import io.runtime.mcumgr.transfer.UploadCallback;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;
import org.jetbrains.annotations.Contract;

@SuppressWarnings("unused")
public class AndroidFileUploader
{
    private Context _context;
    private BluetoothDevice _bluetoothDevice;

    private FsManager _fileSystemManager;
    private McuMgrBleTransport _transport;
    private TransferController _uploadController;
    private FileUploaderCallbackProxy _fileUploaderCallbackProxy;

    private int _lastBytesSent;
    private long _uploadStartTimestampInMs;
    private long _lastBytesSentTimestampInMs;

    private String _resourceId = "";
    private String _remoteFilePathSanitized = "";
    private EAndroidFileUploaderState _currentState = EAndroidFileUploaderState.NONE;

    public AndroidFileUploader() //this flavour is meant to be used in conjunction with trySetBluetoothDevice() and trySetContext()
    {
    }

    public AndroidFileUploader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _context = context;
        _bluetoothDevice = bluetoothDevice;
    }

    public boolean trySetContext(@NonNull final Context context)
    {
        if (!IsIdleOrCold())
            return false;

        if (!tryInvalidateCachedTransport()) //order
            return false;

        _context = context;
        return true;
    }

    public boolean trySetBluetoothDevice(@NonNull final BluetoothDevice bluetoothDevice)
    {
        if (!IsIdleOrCold())
        {
            logMessageAdvertisement("[AFU.TSBD.005] trySetBluetoothDevice() cannot proceed because the uploader is not cold", "FileUploader", "ERROR", _resourceId);
            return false;
        }

        if (!tryInvalidateCachedTransport()) //order
        {
            logMessageAdvertisement("[AFU.TSBD.020] Failed to invalidate the cached-transport instance", "FileUploader", "ERROR", _resourceId);
            return false;
        }

        _bluetoothDevice = bluetoothDevice; //order

        logMessageAdvertisement("[AFU.TSBD.030] Successfully set the android-bluetooth-device to the given value", "FileUploader", "TRACE", _resourceId);

        return true;
    }

    public boolean tryInvalidateCachedTransport()
    {
        if (_transport == null) //already scrapped
            return true;

        if (!IsIdleOrCold()) //if the upload is already in progress we bail out
            return false;

        disposeFilesystemManager(); // order
        disposeTransport(); //         order
        disposeCallbackProxy(); //     order

        return true;
    }

    /**
     * Initiates a file upload asynchronously. The progress is advertised through the callbacks provided by this class.
     * Setup interceptors for them to get informed about the status of the firmware-installation.
     *
     * @param resourceId      the resource-id/nickname/local-file-path of the resource/data that is being
     * @param remoteFilePath  the remote-file-path to save the given data to on the remote device
     * @param data            the bytes to upload
     * @param initialMtuSize  sets the initial MTU for the connection that the McuMgr BLE-transport sets up for the firmware installation that will follow.
     *                        Note that if less than 0 it gets ignored and if it doesn't fall within the range [23, 517] it will cause a hard error.
     * @param windowCapacity  specifies the windows-capacity for the data transfers of the BLE connection - if zero or negative the value provided gets ignored and will be set to 1 by default
     * @param memoryAlignment specifies the memory-alignment to use for the data transfers of the BLE connection - if zero or negative the value provided gets ignored and will be set to 1 by default
     * @return a verdict indicating whether the file uploading was started successfully or not
     */
    public EAndroidFileUploaderVerdict beginUpload(
            final String resourceId,
            final String remoteFilePath,
            final byte[] data,
            final int initialMtuSize,
            final int windowCapacity,
            final int memoryAlignment
    )
    {
        if (!IsCold()) //keep first
        {
            onError("Another upload is already in progress");

            return EAndroidFileUploaderVerdict.FAILED__OTHER_UPLOAD_ALREADY_IN_PROGRESS;
        }

        if (resourceId == null) //can be dud but not null
        {
            onError("Provided resource-id is null", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (remoteFilePath == null || remoteFilePath.isEmpty())
        {
            onError("Provided target-path is empty", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        _resourceId = resourceId;
        _remoteFilePathSanitized = remoteFilePath.trim();
        if (_remoteFilePathSanitized.endsWith("/")) //the path must point to a file not a directory
        {
            onError("Provided target-path points to a directory not a file");

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (!_remoteFilePathSanitized.startsWith("/"))
        {
            onError("Provided target-path is not an absolute path");

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_context == null)
        {
            onError("No context specified - call trySetContext() first");

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_bluetoothDevice == null)
        {
            onError("No bluetooth-device specified - call trySetBluetoothDevice() first");

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (data == null)
        { // data being null is not ok   but data.length==0 is perfectly ok because we might want to create empty files
            onError("Provided data is null");

            return EAndroidFileUploaderVerdict.FAILED__INVALID_DATA;
        }

        try
        {
            resetUploadState(); //order   must be called before ensureTransportIsInitializedExactlyOnce() because the environment might try to set the device via trySetBluetoothDevice()!!!
            ensureTransportIsInitializedExactlyOnce(initialMtuSize); //order
            setLoggingEnabledOnTransport(false); //order

            final EAndroidFileUploaderVerdict verdict = ensureFilesystemManagerIsInitializedExactlyOnce(); //order
            if (verdict != EAndroidFileUploaderVerdict.SUCCESS)
                return verdict;

            requestHighConnectionPriorityOnTransport(); //order
            ensureFileUploaderCallbackProxyIsInitializedExactlyOnce(); //order

            FileUploader fileUploader = new FileUploader( //00
                    _fileSystemManager,
                    _remoteFilePathSanitized,
                    data,
                    Math.max(1, windowCapacity),
                    Math.max(1, memoryAlignment)
            );

            _uploadController = fileUploader.uploadAsync(_fileUploaderCallbackProxy);
        }
        catch (final Exception ex)
        {
            onError("Failed to initialize the upload", ex);

            return EAndroidFileUploaderVerdict.FAILED__ERROR_UPON_COMMENCING;
        }

        return EAndroidFileUploaderVerdict.SUCCESS;

        //00   file-uploader is the new improved way of performing the file upload   it makes use of the window uploading mechanism
        //     aka sending multiple packets without waiting for the response
    }

    private void resetUploadState()
    {
        _lastBytesSent = 0;
        _uploadStartTimestampInMs = 0;
        _lastBytesSentTimestampInMs = 0;

        setState(EAndroidFileUploaderState.IDLE);
        busyStateChangedAdvertisement(true);
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(_resourceId, _remoteFilePathSanitized, 0, 0, 0);
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize)
    {
        if (_transport == null)
        {
            _transport = new McuMgrBleTransport(_context, _bluetoothDevice);
        }

        if (initialMtuSize > 0)
        {
            _transport.setInitialMtu(initialMtuSize);
        }
    }

    private void ensureFileUploaderCallbackProxyIsInitializedExactlyOnce()
    {
        if (_fileUploaderCallbackProxy != null) //already initialized
            return;

        _fileUploaderCallbackProxy = new FileUploaderCallbackProxy();
    }

    private EAndroidFileUploaderVerdict ensureFilesystemManagerIsInitializedExactlyOnce()
    {
        if (_fileSystemManager != null) //already initialized
            return EAndroidFileUploaderVerdict.SUCCESS;

        logMessageAdvertisement("[AFU.EFMIIEO.010] (Re)Initializing filesystem-manager", "FileUploader", "TRACE", _resourceId);

        try
        {
            _fileSystemManager = new FsManager(_transport); //order
        }
        catch (final Exception ex)
        {
            onError("[AFU.EFMIIEO.010] Failed to initialize the native file-system-manager", ex);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidFileUploaderVerdict.SUCCESS;
    }

    public void pause()
    {
        final TransferController transferController = _uploadController;
        if (transferController == null)
            return;

        setState(EAndroidFileUploaderState.PAUSED);
        setLoggingEnabledOnTransport(true);
        transferController.pause();
        busyStateChangedAdvertisement(false);
    }

    public void resume()
    {
        final TransferController transferController = _uploadController;
        if (transferController == null)
            return;

        setState(EAndroidFileUploaderState.UPLOADING);

        busyStateChangedAdvertisement(true);

        setLoggingEnabledOnTransport(false);
        transferController.resume();
    }

    public void disconnect()
    {
        if (_fileSystemManager == null)
            return;

        final McuMgrTransport mcuMgrTransporter = _fileSystemManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        mcuMgrTransporter.release();
    }

    private String _cancellationReason = "";

    public void cancel(final String reason)
    {
        _cancellationReason = reason;

        cancellingAdvertisement(reason); //order
        setState(EAndroidFileUploaderState.CANCELLING); //order
        final TransferController transferController = _uploadController; //order
        if (transferController == null)
            return;

        transferController.cancel(); //order
    }

    private void requestHighConnectionPriorityOnTransport()
    {
        _transport.requestConnPriority(ConnectionPriorityRequest.CONNECTION_PRIORITY_HIGH);
    }

    private void disposeTransport()
    {
        if (_transport == null)
            return;

        try
        {
            _transport.disconnect();
        }
        catch (Exception ex)
        {
            // ignore
        }

        _transport = null;
    }

    private void disposeFilesystemManager()
    {
        if (_fileSystemManager == null)
            return;

        try
        {
            _fileSystemManager.closeAll();
        }
        catch (McuMgrException e)
        {
            // ignore
        }

        _fileSystemManager = null;
    }

    private void disposeCallbackProxy()
    {
        _fileUploaderCallbackProxy = null;
    }

    private void setLoggingEnabledOnTransport(final boolean enabled)
    {
        _transport.setLoggingEnabled(enabled);
    }

    private void setState(final EAndroidFileUploaderState newState)
    {
        if (_currentState == newState)
            return;

        final EAndroidFileUploaderState oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(_resourceId, _remoteFilePathSanitized, oldState, newState); //order

        if (oldState == EAndroidFileUploaderState.IDLE && newState == EAndroidFileUploaderState.UPLOADING)
        {
            fileUploadStartedAdvertisement(_resourceId, _remoteFilePathSanitized);
        }
        else if (oldState == EAndroidFileUploaderState.UPLOADING && newState == EAndroidFileUploaderState.COMPLETE) //00
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(_resourceId, _remoteFilePathSanitized, 100, 0, 0);
        }

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }

    @Contract(pure = true)
    private boolean IsIdleOrCold()
    {
        return _currentState == EAndroidFileUploaderState.IDLE || IsCold();
    }

    @Contract(pure = true)
    private boolean IsCold()
    {
        return _currentState == EAndroidFileUploaderState.NONE // we intentionally omitted the 'idle' state here
                || _currentState == EAndroidFileUploaderState.ERROR
                || _currentState == EAndroidFileUploaderState.COMPLETE
                || _currentState == EAndroidFileUploaderState.CANCELLED;
    }

    private String _lastFatalErrorMessage;

    @Contract(pure = true)
    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    private void onError(final String errorMessage)
    {
        onError(errorMessage, null);
    }

    //@Contract(pure = true) //dont
    public void onError(final String errorMessage, final Exception exception)
    {
        setState(EAndroidFileUploaderState.ERROR);

        fatalErrorOccurredAdvertisement(
                _resourceId,
                _remoteFilePathSanitized,
                errorMessage,
                McuMgrExceptionHelpers.DeduceGlobalErrorCodeFromException(exception)
        );
    }

    public void fatalErrorOccurredAdvertisement(
            final String resourceId,
            final String remoteFilePath,
            final String errorMessage,
            final int globalErrorCode // have a look at EGlobalErrorCode.cs in csharp
    )
    {
        _lastFatalErrorMessage = errorMessage; //this method is meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void busyStateChangedAdvertisement(final boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileUploadStartedAdvertisement(final String resourceId, final String remoteFilePath)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileUploadCompletedAdvertisement(final String resourceId, final String remoteFilePath)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void cancellingAdvertisement(final String reason)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void cancelledAdvertisement(final String reason)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void stateChangedAdvertisement(final String resourceId, final String remoteFilePath, final EAndroidFileUploaderState oldState, final EAndroidFileUploaderState newState) // (final EAndroidFileUploaderState oldState, final EAndroidFileUploaderState newState)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(final String resourceId, final String remoteFilePath, final int progressPercentage, final float currentThroughputInKbps, final float totalAverageThroughputInKbps)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void logMessageAdvertisement(final String message, final String category, final String level, final String resourceId)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    private final class FileUploaderCallbackProxy implements UploadCallback
    {
        @Override
        public void onUploadProgressChanged(final int totalBytesSentSoFar, final int fileSize, final long timestampInMs)
        {
            setState(EAndroidFileUploaderState.UPLOADING);

            int fileUploadProgressPercentage = (int) (totalBytesSentSoFar * 100.f / fileSize);
            float currentThroughputInKbps = calculateCurrentThroughputInKbps(totalBytesSentSoFar, timestampInMs);
            float totalAverageThroughputInKbps = calculateTotalAverageThroughputInKbps(totalBytesSentSoFar, timestampInMs);

            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement( // convert to percent
                    _resourceId,
                    _remoteFilePathSanitized,
                    fileUploadProgressPercentage,
                    currentThroughputInKbps,
                    totalAverageThroughputInKbps
            );
        }

        @SuppressWarnings("DuplicatedCode")
        private float calculateCurrentThroughputInKbps(final int totalBytesSentSoFar, final long timestampInMs) {
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

            float currentThroughputInKbps = ((float) (totalBytesSentSoFar - _lastBytesSent)) / (intervalInSeconds * 1_024); //order

            _lastBytesSent = totalBytesSentSoFar; //order
            _lastBytesSentTimestampInMs = timestampInMs; //order

            return currentThroughputInKbps;
        }

        @SuppressWarnings("DuplicatedCode")
        private float calculateTotalAverageThroughputInKbps(final int totalBytesSentSoFar, final long timestampInMs) {
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

        @Override
        public void onUploadFailed(@NonNull final McuMgrException error)
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(_resourceId, _remoteFilePathSanitized, 0, 0, 0);
            onError(error.getMessage(), error);
            setLoggingEnabledOnTransport(true);
            busyStateChangedAdvertisement(false);

            _uploadController = null; //order
        }

        @Override
        public void onUploadCanceled()
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(_resourceId, _remoteFilePathSanitized, 0, 0, 0);
            setState(EAndroidFileUploaderState.CANCELLED);
            cancelledAdvertisement(_cancellationReason);
            setLoggingEnabledOnTransport(true);
            busyStateChangedAdvertisement(false);

            _uploadController = null; //order
        }

        @Override
        public void onUploadCompleted()
        {
            //fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0, 0); //no need this is taken care of inside setState()
            setState(EAndroidFileUploaderState.COMPLETE);
            fileUploadCompletedAdvertisement(_resourceId, _remoteFilePathSanitized);
            setLoggingEnabledOnTransport(true);
            busyStateChangedAdvertisement(false);

            _uploadController = null; //order
        }
    }
}
