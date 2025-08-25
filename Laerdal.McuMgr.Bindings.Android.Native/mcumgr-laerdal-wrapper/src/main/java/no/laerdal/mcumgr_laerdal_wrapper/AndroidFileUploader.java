package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.FsManager;
import io.runtime.mcumgr.transfer.FileUploader;
import io.runtime.mcumgr.transfer.TransferController;
import io.runtime.mcumgr.transfer.UploadCallback;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;
import org.jetbrains.annotations.Contract;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

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
    private Boolean _currentBusyState = false;
    private EAndroidFileUploaderState _currentState = EAndroidFileUploaderState.NONE;

    private final ExecutorService _backgroundExecutor = Executors.newCachedThreadPool();

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

        if (!tryInvalidateCachedInfrastructure()) //order
            return false;

        _context = context;
        return true;
    }

    public boolean trySetBluetoothDevice(@NonNull final BluetoothDevice bluetoothDevice)
    {
        if (!IsIdleOrCold())
        {
            emitLogEntry("[AFU.TSBD.005] trySetBluetoothDevice() cannot proceed because the uploader is not cold", "file-uploader", EAndroidLoggingLevel.Error, _resourceId);
            return false;
        }

        if (!tryInvalidateCachedInfrastructure()) //order
        {
            emitLogEntry("[AFU.TSBD.020] Failed to invalidate the cached-transport instance", "file-uploader", EAndroidLoggingLevel.Error, _resourceId);
            return false;
        }

        _bluetoothDevice = bluetoothDevice; //order

        emitLogEntry("[AFU.TSBD.030] Successfully set the android-bluetooth-device to the given value", "file-uploader", EAndroidLoggingLevel.Trace, _resourceId);

        return true;
    }

    public boolean tryInvalidateCachedInfrastructure() //must be public
    {
        boolean success1 = tryDisposeFilesystemManager(); // order
        boolean success2 = tryDisposeTransport(); //         order
        boolean success3 = tryDisposeCallbackProxy(); //     order

        return success1 && success2 && success3;
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

            setState(EAndroidFileUploaderState.IDLE, 0); //order
            FileUploader fileUploader = new FileUploader( //00  order
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
        _uploadStartTimestampInMs = 0;

        _lastBytesSent = 0;
        _lastBytesSentTimestampInMs = 0;

        setState(EAndroidFileUploaderState.NONE);
        setBusyState(false);
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize)
    {
        if (_transport == null)
        {
            emitLogEntry("[AFD.ETIIEO.000] Transport is null - instantiating it now", "firmware-uploader", EAndroidLoggingLevel.Warning, _resourceId);

            _transport = new McuMgrBleTransport(_context, _bluetoothDevice);
        }

        if (initialMtuSize > 0)
        {
            _transport.setInitialMtu(initialMtuSize);
            emitLogEntry("[AFD.ETIIEO.010] Initial-MTU-size set explicitly to '" + initialMtuSize + "'", "firmware-uploader", EAndroidLoggingLevel.Info, _resourceId);
        }
        else
        {
            emitLogEntry("[AFD.ETIIEO.020] Initial-MTU-size left to its nordic-default-value which is probably 498", "firmware-uploader", EAndroidLoggingLevel.Info, _resourceId);
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

        emitLogEntry("[AFU.EFMIIEO.010] (Re)Initializing filesystem-manager", "file-uploader", EAndroidLoggingLevel.Trace, _resourceId);

        try
        {
            _fileSystemManager = new FsManager(_transport); //order
        }
        catch (final Exception ex)
        {
            onError("[AFU.EFMIIEO.020] Failed to initialize the native file-system-manager", ex); //sets the state to ERROR too!
            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidFileUploaderVerdict.SUCCESS;
    }

    public boolean tryPause()
    {
        if (_currentState == EAndroidFileUploaderState.PAUSED)
            return true; // already paused which is ok

        if (_currentState != EAndroidFileUploaderState.UPLOADING)
            return false;

        final TransferController transferController = _uploadController;
        if (transferController == null)
            return false; //controller has been trashed

        try
        {
            transferController.pause();

            setState(EAndroidFileUploaderState.PAUSED);
            setBusyState(false);

            setLoggingEnabledOnTransport(true);

            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFU.TP.010] [SUPPRESSED] Error while trying to pause the upload:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
            return false;
        }
    }

    public boolean tryResume()
    {
        if (_currentState == EAndroidFileUploaderState.UPLOADING)
            return true; //already downloading which is ok

        if (_currentState != EAndroidFileUploaderState.PAUSED)
            return false;

        final TransferController transferController = _uploadController;
        if (transferController == null)
            return false; //controller has been trashed

        try
        {
            transferController.resume();

            setState(EAndroidFileUploaderState.UPLOADING);
            setBusyState(true);

            setLoggingEnabledOnTransport(false);

            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFU.TR.010] [SUPPRESSED] Error while trying to resume the upload:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
            return false;
        }
    }

    public void nativeDispose()
    {
        emitLogEntry("[AFU.ND.010] Disposing the native-file-uploader", "file-uploader", EAndroidLoggingLevel.Trace, _resourceId);
        
        tryInvalidateCachedInfrastructure(); //  doesnt throw
        tryShutdownBackgroundExecutor(); //      doesnt throw
    }

    @SuppressWarnings("UnusedReturnValue")
    private boolean tryShutdownBackgroundExecutor()
    {
        emitLogEntry("[AFU.TSBE.010] Shutting down the background-executor ...", "file-uploader", EAndroidLoggingLevel.Trace, _resourceId);

        try
        {
            _backgroundExecutor.shutdown();
            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFU.TBE.010] [SUPPRESSED] Error while shutting down background executor:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
            return false;
        }
    }

    @SuppressWarnings("UnusedReturnValue")
    public boolean tryDisconnect() //doesnt throw
    {
        emitLogEntry("[AFU.TDISC.010] Will try to disconnect now ...", "file-uploader", EAndroidLoggingLevel.Trace, _resourceId);

        if (_transport == null)
        {
            emitLogEntry("[AFU.TDISC.020] Transport is null so nothing to disconnect from", "file-uploader", EAndroidLoggingLevel.Trace, _resourceId);
            return true;
        }

        try
        {
            _transport.release();
            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFU.TDISC.010] [SUPPRESSED] Error while disposing transport:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
            return false;
        }
    }

    private String _cancellationReason = "";

    public boolean tryCancel(final String reason)
    {
        _cancellationReason = reason;

        fireAndForgetInTheBg(() -> cancellingAdvertisement(reason)); //      order
        setState(EAndroidFileUploaderState.CANCELLING); //                   order

        final TransferController transferController = _uploadController; //  order
        if (transferController == null)
            return true; // nothing to cancel which is not an error

        try
        {
            transferController.cancel(); //order
            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFU.TC.010] [SUPPRESSED] Error while trying to cancel the upload:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
            return false;
        }
    }

    private void requestHighConnectionPriorityOnTransport()
    {
        _transport.requestConnPriority(ConnectionPriorityRequest.CONNECTION_PRIORITY_HIGH);
    }

    @SuppressWarnings("UnusedReturnValue")
    private boolean tryDisposeTransport()
    {
        if (_transport == null)
            return true; // already disposed

        boolean success = true;
        try
        {
            _transport.release();
        }
        catch (final Exception ex) // suppress
        {
            success = false;
            emitLogEntry("[AFU.TDT.010] [SUPPRESSED] Error while disposing transport:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
        }

        _transport = null;
        return success;
    }

    @SuppressWarnings({"UnusedReturnValue", "DuplicatedCode"})
    private boolean tryDisposeFilesystemManager()
    {
        if (_fileSystemManager == null)
            return true;

        boolean success = true;
        try
        {
            _fileSystemManager.closeAll();
        }
        catch (final Exception ex)
        {
            success = false;
            emitLogEntry("[AFU.TDFM.010] [SUPPRESSED] Error while closing the file-system-manager:\n\n" + ex, "file-uploader", EAndroidLoggingLevel.Warning, _resourceId);
        }

        _fileSystemManager = null;
        return success;
    }

    private boolean tryDisposeCallbackProxy()
    {
        _fileUploaderCallbackProxy = null;
        return true;
    }

    private void setLoggingEnabledOnTransport(final boolean enabled)
    {
        if (_transport == null)
            return;

        _transport.setLoggingEnabled(enabled);
    }

    private void setBusyState(final boolean newBusyState)
    {
        if (_currentBusyState == newBusyState)
            return;

        _currentBusyState = newBusyState;

        fireAndForgetInTheBg(() -> busyStateChangedAdvertisement(newBusyState));
    }

    private void setState(final EAndroidFileUploaderState newState)
    {
        setState(newState, 0);
    }

    private void setState(final EAndroidFileUploaderState newState, long totalBytesToBeUploaded)
    {
        if (_currentState == newState)
            return;

        final EAndroidFileUploaderState oldState = _currentState; //order

        _currentState = newState; //order

        final String resourceIdSnapshot = _resourceId;
        final String remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized;

        fireAndForgetInTheBg(() -> {
            stateChangedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, oldState, newState); //order

            switch (newState)
            {
                case NONE: // * -> none
                    fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, 0, 0, 0);
                    break;

                case UPLOADING: // idle/paused -> uploading
                    if (oldState != EAndroidFileUploaderState.IDLE && oldState != EAndroidFileUploaderState.PAUSED)
                    {
                        logMessageAdvertisement("[AFU.SS.FAFITB.010] State changed to 'uploading' from an unexpected state '" + oldState + "' - this looks fishy so report this incident!", "file-uploader", EAndroidLoggingLevel.Warning.toString(), resourceIdSnapshot);
                    }

                    if (oldState != EAndroidFileUploaderState.PAUSED) //todo   if the previous state is 'paused' we should raise the event 'FileUploadResumingNow'
                    {
                        logMessageAdvertisement("[AFU.SS.FAFITB.025] Starting uploading of '" + totalBytesToBeUploaded + "' bytes", "file-uploader", EAndroidLoggingLevel.Info.toString(), resourceIdSnapshot);

                        fileUploadStartedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, totalBytesToBeUploaded);
                    }
                    break;

                case COMPLETE: // uploading -> complete
                    logMessageAdvertisement("[AFU.SS.FAFITB.030] Completed uploading of '" + totalBytesToBeUploaded + "' bytes", "file-uploader", EAndroidLoggingLevel.Info.toString(), resourceIdSnapshot);

                    if (oldState != EAndroidFileUploaderState.UPLOADING)
                    {
                        logMessageAdvertisement("[AFU.SS.FAFITB.035] State changed to 'complete' from an unexpected state '" + oldState + "' - this looks fishy so report this incident!", "file-uploader", EAndroidLoggingLevel.Warning.toString(), resourceIdSnapshot);
                    }

                    fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot, 100, 0, 0); //00   order
                    fileUploadCompletedAdvertisement(resourceIdSnapshot, remoteFilePathSanitizedSnapshot); //                                                 order
                    break;
            }
        });

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
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

    //@Contract(pure = true) //dont
    private void onError(final String errorMessage)
    {
        onError(errorMessage, null);
    }

    //@Contract(pure = true) //dont
    private void onError(final String errorMessage, final Exception exception)
    {
        setState(EAndroidFileUploaderState.ERROR);

        _lastFatalErrorMessage = errorMessage;

        final String resourceIdSnapshot = _resourceId;
        final String remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized;
        fireAndForgetInTheBg(() -> fatalErrorOccurredAdvertisement(
                resourceIdSnapshot,
                remoteFilePathSanitizedSnapshot,
                errorMessage,
                McuMgrExceptionHelpers.DeduceGlobalErrorCodeFromException(exception)
        ));
    }

    public void fatalErrorOccurredAdvertisement(
            final String resourceId,
            final String remoteFilePath,
            final String errorMessage,
            final int globalErrorCode // have a look at EGlobalErrorCode.cs in csharp
    )
    {
        //this method is meant to be overridden by csharp binding libraries to intercept updates
    }

    //@Contract(pure = true) //dont
    private void emitLogEntry(final String message, final String category, final EAndroidLoggingLevel level, final String resourceId)
    {
        fireAndForgetInTheBg(() -> logMessageAdvertisement(message, category, level.toString(), resourceId));
    }

    @Contract(pure = true)
    public void busyStateChangedAdvertisement(final boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileUploadStartedAdvertisement(final String resourceId, final String remoteFilePath, final long totalBytesToBeUploaded)
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
    public void fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(final String resourceId, final String remoteFilePath, final int progressPercentage, final float currentThroughputInKBps, final float totalAverageThroughputInKBps)
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
        public void onUploadProgressChanged(final int totalBytesSentSoFar, final int totalBytesToBeUploaded, final long timestampInMs)
        {
            setState(EAndroidFileUploaderState.UPLOADING, totalBytesToBeUploaded);
            setBusyState(true);

            final String resourceIdSnapshot = _resourceId; //order
            final String remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized; //order
            fireAndForgetInTheBg(() -> {
                int fileUploadProgressPercentage = (int) (totalBytesSentSoFar * 100.f / totalBytesToBeUploaded);
                float currentThroughputInKBps = calculateCurrentThroughputInKBps(totalBytesSentSoFar, timestampInMs);
                float totalAverageThroughputInKBps = calculateTotalAverageThroughputInKBps(totalBytesSentSoFar, timestampInMs);

                fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(
                        resourceIdSnapshot,
                        remoteFilePathSanitizedSnapshot,
                        fileUploadProgressPercentage,
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

        @Override
        public void onUploadFailed(@NonNull final McuMgrException error)
        {
            onError(error.getMessage(), error);
            setLoggingEnabledOnTransport(true);

            setBusyState(false);

            _uploadController = null; //order
        }

        @Override
        public void onUploadCanceled()
        {
            setState(EAndroidFileUploaderState.CANCELLED); //                             order
            fireAndForgetInTheBg(() -> cancelledAdvertisement(_cancellationReason)); //   order
            setBusyState(false); //                                                       order

            setLoggingEnabledOnTransport(true);

            _uploadController = null; //order
        }

        @Override
        public void onUploadCompleted()
        {
            //fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0, 0); //no need this is taken care of inside setState()

            setState(EAndroidFileUploaderState.COMPLETE); // order
            setBusyState(false); //                          order

            setLoggingEnabledOnTransport(true);

            _uploadController = null; //order
        }
    }
}
