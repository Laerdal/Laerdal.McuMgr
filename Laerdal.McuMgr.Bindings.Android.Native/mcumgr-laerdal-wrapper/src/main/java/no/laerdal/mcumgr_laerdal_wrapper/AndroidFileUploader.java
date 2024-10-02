package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrErrorException;
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

    private int _initialBytes;
    private long _uploadStartTimestamp;
    private String _remoteFilePathSanitized;
    private EAndroidFileUploaderState _currentState = EAndroidFileUploaderState.NONE;

    public AndroidFileUploader()
    {
    }

    public AndroidFileUploader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _context = context;
        _bluetoothDevice = bluetoothDevice;
    }

    public boolean trySetContext(@NonNull final Context context)
    {
        if (!IsCold())
            return false;

        if (!tryInvalidateCachedTransport()) //order
            return false;

        _context = context;
        return true;
    }

    public boolean trySetBluetoothDevice(@NonNull final BluetoothDevice bluetoothDevice)
    {
        if (!IsCold())
            return false;

        if (!tryInvalidateCachedTransport()) //order
            return false;

        _bluetoothDevice = bluetoothDevice; //order
        return true;
    }

    public boolean tryInvalidateCachedTransport()
    {
        if (_transport == null) //already scrapped
            return true;

        if (!IsCold()) //if the upload is already in progress we bail out
            return false;

        disposeFilesystemManager(); // order
        disposeTransport(); //         order

        return true;
    }

    /**
     * Initiates a file upload asynchronously. The progress is advertised through the callbacks provided by this class.
     * Setup interceptors for them to get informed about the status of the firmware-installation.
     *
     * @param remoteFilePath the remote-file-path to save the given data to on the remote device
     * @param data the bytes to upload
     * @param initialMtuSize sets the initial MTU for the connection that the McuMgr BLE-transport sets up for the firmware installation that will follow.
     *                       Note that if less than 0 it gets ignored and if it doesn't fall within the range [23, 517] it will cause a hard error.
     * @param windowCapacity specifies the windows-capacity for the data transfers of the BLE connection - if zero or negative the value provided gets ignored and will be set to 1 by default
     * @param memoryAlignment specifies the memory-alignment to use for the data transfers of the BLE connection - if zero or negative the value provided gets ignored and will be set to 1 by default
     *
     * @return a verdict indicating whether the file uploading was started successfully or not
     */
    public EAndroidFileUploaderVerdict beginUpload(
            final String remoteFilePath,
            final byte[] data,
            final int initialMtuSize,
            final int windowCapacity,
            final int memoryAlignment
    )
    {
        if (!IsCold()) {
            setState(EAndroidFileUploaderState.ERROR);
            onError("N/A", "Another upload is already in progress", null);

            return EAndroidFileUploaderVerdict.FAILED__OTHER_UPLOAD_ALREADY_IN_PROGRESS;
        }

        if (_context == null) {
            setState(EAndroidFileUploaderState.ERROR);
            onError("N/A", "No context specified - call trySetContext() first", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_bluetoothDevice == null) {
            setState(EAndroidFileUploaderState.ERROR);
            onError("N/A", "No bluetooth-device specified - call trySetBluetoothDevice() first", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (remoteFilePath == null || remoteFilePath.isEmpty()) {
            setState(EAndroidFileUploaderState.ERROR);
            onError("N/A", "Provided target-path is empty", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        _remoteFilePathSanitized = remoteFilePath.trim();
        if (_remoteFilePathSanitized.endsWith("/")) //the path must point to a file not a directory
        {
            setState(EAndroidFileUploaderState.ERROR);
            onError(_remoteFilePathSanitized, "Provided target-path points to a directory not a file", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (!_remoteFilePathSanitized.startsWith("/"))
        {
            setState(EAndroidFileUploaderState.ERROR);
            onError(_remoteFilePathSanitized, "Provided target-path is not an absolute path", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (data == null) { // data being null is not ok   but data.length==0 is perfectly ok because we might want to create empty files
            setState(EAndroidFileUploaderState.ERROR);
            onError(_remoteFilePathSanitized, "Provided data is null", null);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_DATA;
        }

        ensureTransportIsInitializedExactlyOnce(initialMtuSize); //order

        final EAndroidFileUploaderVerdict verdict = ensureFilesystemManagerIsInitializedExactlyOnce(); //order
        if (verdict != EAndroidFileUploaderVerdict.SUCCESS)
            return verdict;

        ensureFileUploaderCallbackProxyIsInitializedExactlyOnce(); //order

        resetUploadState(); //order
        setLoggingEnabled(false);

        try
        {
            _uploadController = new FileUploader( //00
                    _fileSystemManager,
                    _remoteFilePathSanitized,
                    data,
                    Math.max(1, windowCapacity),
                    Math.max(1, memoryAlignment)
            ).uploadAsync(_fileUploaderCallbackProxy);
        }
        catch (final Exception ex)
        {
            setState(EAndroidFileUploaderState.ERROR);
            onError(_remoteFilePathSanitized, "Failed to initialize the upload", ex);

            return EAndroidFileUploaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidFileUploaderVerdict.SUCCESS;

        //00   file-uploader is the new improved way of performing the file upload   it makes use of the window uploading mechanism
        //     aka sending multiple packets without waiting for the response
    }

    private void resetUploadState() {
        _initialBytes = 0;
        _uploadStartTimestamp = 0;

        setState(EAndroidFileUploaderState.IDLE);
        busyStateChangedAdvertisement(true);
        fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize)
    {
        if (_transport != null)
            return;

        _transport = new McuMgrBleTransport(_context, _bluetoothDevice);

        if (initialMtuSize > 0)
        {
            _transport.setInitialMtu(initialMtuSize);
        }
    }

    private void ensureFileUploaderCallbackProxyIsInitializedExactlyOnce() {
        if (_fileUploaderCallbackProxy != null) //already initialized
            return;

        _fileUploaderCallbackProxy = new FileUploaderCallbackProxy();
    }

    private EAndroidFileUploaderVerdict ensureFilesystemManagerIsInitializedExactlyOnce() {
        if (_fileSystemManager != null) //already initialized
            return EAndroidFileUploaderVerdict.SUCCESS;

        try
        {
            _fileSystemManager = new FsManager(_transport); //order

            requestHighConnectionPriority(_fileSystemManager); //order
        }
        catch (final Exception ex)
        {
            setState(EAndroidFileUploaderState.ERROR);
            onError(_remoteFilePathSanitized, ex.getMessage(), ex);

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
        setLoggingEnabled(true);
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
        _initialBytes = 0;

        setLoggingEnabled(false);
        transferController.resume();
    }

    public void disconnect() {
        if (_fileSystemManager == null)
            return;

        final McuMgrTransport mcuMgrTransporter = _fileSystemManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        mcuMgrTransporter.release();
    }

    public void cancel()
    {
        setState(EAndroidFileUploaderState.CANCELLING); //order

        final TransferController transferController = _uploadController;
        if (transferController == null)
            return;

        transferController.cancel(); //order
    }

    static private void requestHighConnectionPriority(final FsManager fileSystemManager)
    {
        final McuMgrTransport mcuMgrTransporter = fileSystemManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        final McuMgrBleTransport bleTransporter = (McuMgrBleTransport) mcuMgrTransporter;
        bleTransporter.requestConnPriority(ConnectionPriorityRequest.CONNECTION_PRIORITY_HIGH);
    }

    private void disposeTransport()
    {
        if (_transport == null)
            return;

        try {
            _transport.disconnect();
        } catch (Exception ex) {
            // ignore
        }

        _transport = null;
    }

    private void disposeFilesystemManager()
    {
        if (_fileSystemManager == null)
            return;

        try {
            _fileSystemManager.closeAll();
        } catch (McuMgrException e) {
            // ignore
        }

        _fileSystemManager = null;
    }

    private void setLoggingEnabled(final boolean enabled)
    {
        final McuMgrTransport mcuMgrTransporter = _fileSystemManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        ((McuMgrBleTransport) mcuMgrTransporter).setLoggingEnabled(enabled);
    }

    private void setState(final EAndroidFileUploaderState newState)
    {
        final EAndroidFileUploaderState oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(_remoteFilePathSanitized, oldState, newState); //order

        if (oldState == EAndroidFileUploaderState.UPLOADING && newState == EAndroidFileUploaderState.COMPLETE) //00
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0);
        }

        //00 trivial hotfix to deal with the fact that the file-upload progress% doesn't fill up to 100%
    }

    @Contract(pure = true)
    private boolean IsCold()
    {
        return _currentState == EAndroidFileUploaderState.NONE
                || _currentState == EAndroidFileUploaderState.ERROR
                || _currentState == EAndroidFileUploaderState.COMPLETE
                || _currentState == EAndroidFileUploaderState.CANCELLED;
    }

    private String _lastFatalErrorMessage;

    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    public void onError(
            final String remoteFilePath,
            final String errorMessage,
            final Exception exception
    )
    {
        if (!(exception instanceof McuMgrErrorException))
        {
            fatalErrorOccurredAdvertisement(remoteFilePath, errorMessage, 0, 0);
            return;
        }

        McuMgrErrorException mcuMgrErrorException = (McuMgrErrorException) exception;
        fatalErrorOccurredAdvertisement(
                remoteFilePath,
                errorMessage,
                mcuMgrErrorException.getCode().value(),
                (mcuMgrErrorException.getGroupCode() != null ? mcuMgrErrorException.getGroupCode().group : -99)
        );
    }

    public void fatalErrorOccurredAdvertisement(
            final String remoteFilePath,
            final String errorMessage,
            final int mcuMgrErrorCode, //         io.runtime.mcumgr.McuMgrErrorCode
            final int fsManagerGroupReturnCode // io.runtime.mcumgr.managers.FsManager.ReturnCode
    )
    {
        _lastFatalErrorMessage = errorMessage; //this method is meant to be overridden by csharp binding libraries to intercept updates
    }

    public void busyStateChangedAdvertisement(final boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void fileUploadedAdvertisement(final String remoteFilePath)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void cancelledAdvertisement()
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void stateChangedAdvertisement(final String remoteFilePath, final EAndroidFileUploaderState oldState, final EAndroidFileUploaderState newState) // (final EAndroidFileUploaderState oldState, final EAndroidFileUploaderState newState)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(final int progressPercentage, final float averageThroughput)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void logMessageAdvertisement(final String remoteFilePath, final String message, final String category, final String level)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    private final class FileUploaderCallbackProxy implements UploadCallback
    {
        @Override
        public void onUploadProgressChanged(final int bytesSent, final int fileSize, final long timestamp)
        {
            setState(EAndroidFileUploaderState.UPLOADING);

            float transferSpeed = 0;
            int fileUploadProgressPercentage = 0;

            if (_initialBytes == 0)
            {
                _initialBytes = bytesSent;
                _uploadStartTimestamp = timestamp;
            }
            else
            {
                final int bytesSentSinceUploadStarted = bytesSent - _initialBytes;
                final long timeSinceUploadStarted = timestamp - _uploadStartTimestamp; // bytes/ms = KB/s

                transferSpeed = (float) bytesSentSinceUploadStarted / (float) timeSinceUploadStarted;
                fileUploadProgressPercentage = (int) (bytesSent * 100.f / fileSize);
            }

            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement( // convert to percent
                    fileUploadProgressPercentage,
                    transferSpeed
            );
        }

        @Override
        public void onUploadFailed(@NonNull final McuMgrException error)
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
            setState(EAndroidFileUploaderState.ERROR);
            onError(_remoteFilePathSanitized, error.getMessage(), error);
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);

            _uploadController = null; //order
        }

        @Override
        public void onUploadCanceled()
        {
            fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
            setState(EAndroidFileUploaderState.CANCELLED);
            cancelledAdvertisement();
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);

            _uploadController = null; //order
        }

        @Override
        public void onUploadCompleted()
        {
            //fileUploadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0); //no need this is taken care of inside setState()
            setState(EAndroidFileUploaderState.COMPLETE);
            fileUploadedAdvertisement(_remoteFilePathSanitized);
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);

            _uploadController = null; //order
        }
    }
}
