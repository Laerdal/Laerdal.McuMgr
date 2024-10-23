package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrErrorException;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.FsManager;
import io.runtime.mcumgr.transfer.DownloadCallback;
import io.runtime.mcumgr.transfer.TransferController;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;
import org.jetbrains.annotations.Contract;
import org.jetbrains.annotations.NotNull;

@SuppressWarnings("unused")
public class AndroidFileDownloader
{
    private Context _context;
    private BluetoothDevice _bluetoothDevice;

    private FsManager _fileSystemManager;
    private McuMgrBleTransport _transport;
    private TransferController _downloadingController;
    private FileDownloaderCallbackProxy _fileDownloaderCallbackProxy;

    private int _initialBytes;
    private long _downloadStartTimestamp;
    private String _remoteFilePathSanitized = "";
    private EAndroidFileDownloaderState _currentState = EAndroidFileDownloaderState.NONE;

    public AndroidFileDownloader() //this flavour is meant to be used in conjunction with trySetBluetoothDevice() and trySetContext()
    {
    }

    public AndroidFileDownloader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
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
        if (!IsIdleOrCold()) {
            logMessageAdvertisement("[AFD.TSBD.005] trySetBluetoothDevice() cannot proceed because the uploader is not cold", "FileUploader", "ERROR", _remoteFilePathSanitized);
            return false;
        }

        if (!tryInvalidateCachedTransport()) //order
        {
            logMessageAdvertisement("[AFD.TSBD.020] Failed to invalidate the cached-transport instance", "FileUploader", "ERROR", _remoteFilePathSanitized);
            return false;
        }

        _bluetoothDevice = bluetoothDevice; //order

        logMessageAdvertisement("[AFD.TSBD.030] Successfully set the android-bluetooth-device to the given value", "FileUploader", "TRACE", _remoteFilePathSanitized);

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
     * Initiates a file download asynchronously. The progress is advertised through the callbacks provided by this class.
     * Setup interceptors for them to get informed about the status of the firmware-installation.
     *
     * @param remoteFilePath the remote-file-path to the file on the remote device that you wish to download
     * @param initialMtuSize sets the initial MTU for the connection that the McuMgr BLE-transport sets up for the firmware installation that will follow.
     *                       Note that if less than 0 it gets ignored and if it doesn't fall within the range [23, 517] it will cause a hard error.
     *
     * @return a verdict indicating whether the file uploading was started successfully or not
     */
    public EAndroidFileDownloaderVerdict beginDownload(
            final String remoteFilePath,
            final int initialMtuSize
            // final int windowCapacity, //theoretically nordic firmwares at some point will support this for downloads   but as of Q3 2024 there is no support for this
    )
    {
        if (!IsCold()) //keep first
        {
            onError("Another download is already in progress");

            return EAndroidFileDownloaderVerdict.FAILED__DOWNLOAD_ALREADY_IN_PROGRESS;
        }

        if (remoteFilePath == null || remoteFilePath.isEmpty()) {
            onError("Target-file provided is dud!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        _remoteFilePathSanitized = remoteFilePath.trim();
        if (_remoteFilePathSanitized.endsWith("/")) //the path must point to a file not a directory
        {
            onError("Provided target-path points to a directory not a file");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (!_remoteFilePathSanitized.startsWith("/"))
        {
            onError("Provided target-path is not an absolute path");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_context == null) {
            onError("No context specified - call trySetContext() first");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_bluetoothDevice == null) {
            onError("No bluetooth-device specified - call trySetBluetoothDevice() first");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        resetDownloadState(); //order   must be called before ensureTransportIsInitializedExactlyOnce() because the environment might try to set the device via trySetBluetoothDevice()!!!
        ensureTransportIsInitializedExactlyOnce(initialMtuSize); //order

        final EAndroidFileDownloaderVerdict verdict = ensureFilesystemManagerIsInitializedExactlyOnce(); //order
        if (verdict != EAndroidFileDownloaderVerdict.SUCCESS)
            return verdict;

        ensureFileDownloaderCallbackProxyIsInitializedExactlyOnce(); //order

        setLoggingEnabled(false);
        try
        {
            _downloadingController = _fileSystemManager.fileDownload(_remoteFilePathSanitized, _fileDownloaderCallbackProxy);
        }
        catch (final Exception ex)
        {
            onError("Failed to initialize download", ex);

            return EAndroidFileDownloaderVerdict.FAILED__ERROR_UPON_COMMENCING;
        }

        return EAndroidFileDownloaderVerdict.SUCCESS;
    }

    public void pause()
    {
        final TransferController transferController = _downloadingController;
        if (transferController == null)
            return;

        setState(EAndroidFileDownloaderState.PAUSED);
        setLoggingEnabled(true);
        transferController.pause();
        busyStateChangedAdvertisement(false);
    }

    public void resume()
    {
        final TransferController transferController = _downloadingController;
        if (transferController == null)
            return;

        setState(EAndroidFileDownloaderState.DOWNLOADING);

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
        setState(EAndroidFileDownloaderState.CANCELLING); //order

        final TransferController transferController = _downloadingController;
        if (transferController == null)
            return;

        transferController.cancel(); //order
    }

    private void resetDownloadState() {
        _initialBytes = 0;
        _downloadStartTimestamp = 0;

        setState(EAndroidFileDownloaderState.IDLE);
        busyStateChangedAdvertisement(true);
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize)
    {
        if (_transport != null)
            return;

        logMessageAdvertisement("[AFD.ETIIEO.010] (Re)Initializing transport: initial-mtu-size=" + initialMtuSize, "FileDownloader", "TRACE", _remoteFilePathSanitized);

        _transport = new McuMgrBleTransport(_context, _bluetoothDevice);

        if (initialMtuSize > 0)
        {
            _transport.setInitialMtu(initialMtuSize);
        }
    }

    private void ensureFileDownloaderCallbackProxyIsInitializedExactlyOnce() {
        if (_fileDownloaderCallbackProxy != null) //already initialized
            return;

        _fileDownloaderCallbackProxy = new FileDownloaderCallbackProxy();
    }

    private EAndroidFileDownloaderVerdict ensureFilesystemManagerIsInitializedExactlyOnce() {
        if (_fileSystemManager != null) //already initialized
            return EAndroidFileDownloaderVerdict.SUCCESS;

        logMessageAdvertisement("[AFD.EFMIIEO.010] (Re)Initializing filesystem-manager", "FileDownloader", "TRACE", _remoteFilePathSanitized);

        try
        {
            _fileSystemManager = new FsManager(_transport); //order

            requestHighConnectionPriority(_fileSystemManager); //order
        }
        catch (final Exception ex)
        {
            onError(ex.getMessage(), ex);

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidFileDownloaderVerdict.SUCCESS;
    }

    private void requestHighConnectionPriority(FsManager fileSystemManager)
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

    private void disposeCallbackProxy()
    {
        _fileDownloaderCallbackProxy = null;
    }

    private void setLoggingEnabled(final boolean enabled)
    {
        final McuMgrTransport mcuMgrTransporter = _fileSystemManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        ((McuMgrBleTransport) mcuMgrTransporter).setLoggingEnabled(enabled);
    }

    private void setState(final EAndroidFileDownloaderState newState)
    {
        final EAndroidFileDownloaderState oldState = _currentState; //order

        _currentState = newState; //order

        stateChangedAdvertisement(oldState, newState); //order

        if (oldState == EAndroidFileDownloaderState.DOWNLOADING && newState == EAndroidFileDownloaderState.COMPLETE) //00
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0);
        }

        //00 trivial hotfix to deal with the fact that the filedownload progress% doesnt fill up to 100%
    }

    @Contract(pure = true)
    private boolean IsIdleOrCold()
    {
        return _currentState == EAndroidFileDownloaderState.IDLE || IsCold();
    }

    @Contract(pure = true)
    private boolean IsCold()
    {
        return _currentState == EAndroidFileDownloaderState.NONE
                || _currentState == EAndroidFileDownloaderState.ERROR
                || _currentState == EAndroidFileDownloaderState.COMPLETE
                || _currentState == EAndroidFileDownloaderState.CANCELLED;
    }

    private String _lastFatalErrorMessage;

    @Contract(pure = true)
    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    public void onError(final String errorMessage)
    {
        onError(errorMessage, null);
    }

    //@Contract(pure = true) //dont
    public void onError(final String errorMessage, final Exception exception)
    {
        setState(EAndroidFileDownloaderState.ERROR);

        if (!(exception instanceof McuMgrErrorException))
        {
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, errorMessage, -1, -1);
            return;
        }

        McuMgrErrorException mcuMgrErrorException = (McuMgrErrorException) exception;
        fatalErrorOccurredAdvertisement(
                _remoteFilePathSanitized,
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
        _lastFatalErrorMessage = errorMessage;
    }

    @Contract(pure = true)
    public void busyStateChangedAdvertisement(boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void cancelledAdvertisement()
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true) //wrapper utility method so that we wont have to constantly pass remoteFilePathSanitized as the first argument    currently unused but should be handy in the future
    public void stateChangedAdvertisement(final EAndroidFileDownloaderState oldState, final EAndroidFileDownloaderState newState)
    {
        stateChangedAdvertisement(_remoteFilePathSanitized, oldState, newState);
    }

    @Contract(pure = true)
    public void stateChangedAdvertisement(final String resource, final EAndroidFileDownloaderState oldState, final EAndroidFileDownloaderState newState)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(final int progressPercentage, final float averageThroughput)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void downloadCompletedAdvertisement(final String resource, final byte[] data)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true) //wrapper utility method so that we wont have to constantly pass remoteFilePathSanitized as the fourth argument    currently unused but should be handy in the future
    private void logMessageAdvertisement(final String message, final String category, final String level)
    {
        logMessageAdvertisement(message, category, level, _remoteFilePathSanitized);
    }

    @Contract(pure = true)
    public void logMessageAdvertisement(final String message, final String category, final String level, final String resource) //wrapper method
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    private final class FileDownloaderCallbackProxy implements DownloadCallback
    {
        @Override
        public void onDownloadProgressChanged(final int bytesSent, final int fileSize, final long timestamp)
        {
            setState(EAndroidFileDownloaderState.DOWNLOADING);

            float transferSpeed = 0;
            int fileDownloadProgressPercentage = 0;

            if (_initialBytes == 0)
            {
                _initialBytes = bytesSent;
                _downloadStartTimestamp = timestamp;
            }
            else
            {
                final int bytesSentSinceDownloadStarted = bytesSent - _initialBytes;
                final long timeSinceDownloadStarted = timestamp - _downloadStartTimestamp; // bytes/ms = KB/s

                transferSpeed = (float) bytesSentSinceDownloadStarted / (float) timeSinceDownloadStarted;
                fileDownloadProgressPercentage = (int) (bytesSent * 100.f / fileSize);
            }

            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement( // convert to percent
                    fileDownloadProgressPercentage,
                    transferSpeed
            );
        }

        @Override
        public void onDownloadFailed(@NonNull final McuMgrException exception)
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
            onError(exception.getMessage(), exception);
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);

            _downloadingController = null; //game over
        }

        @Override
        public void onDownloadCanceled()
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
            setState(EAndroidFileDownloaderState.CANCELLED);
            cancelledAdvertisement();
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);

            _downloadingController = null; //game over
        }

        @Override
        public void onDownloadCompleted(byte @NotNull [] data)
        {
            //fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0); //no need this is taken care of inside setState()

            setState(EAndroidFileDownloaderState.COMPLETE); //                    order  vital
            downloadCompletedAdvertisement(_remoteFilePathSanitized, data); //    order  vital

            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);

            _downloadingController = null; //game over
        }
    }
}
