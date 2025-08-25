package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrErrorCode;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.FsManager;
import io.runtime.mcumgr.response.HasReturnCode;
import io.runtime.mcumgr.transfer.DownloadCallback;
import io.runtime.mcumgr.transfer.TransferController;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;
import org.jetbrains.annotations.Contract;
import org.jetbrains.annotations.NotNull;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

@SuppressWarnings("unused")
public class AndroidFileDownloader
{
    private Context _context;
    private BluetoothDevice _bluetoothDevice;

    private FsManager _fileSystemManager;
    private McuMgrBleTransport _transport;
    private TransferController _downloadingController;
    private FileDownloaderCallbackProxy _fileDownloaderCallbackProxy;

    private int _lastBytesSent;
    private long _lastBytesSentTimestampInMs;
    private long _downloadStartTimestampInMs;

    private String _remoteFilePathSanitized = "";
    private Boolean _currentBusyState = false;
    private EAndroidFileDownloaderState _currentState = EAndroidFileDownloaderState.NONE;

    private final ExecutorService _backgroundExecutor = Executors.newCachedThreadPool();

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

        if (!tryInvalidateCachedInfrastructure()) //order
            return false;

        _context = context;
        return true;
    }

    public boolean trySetBluetoothDevice(@NonNull final BluetoothDevice bluetoothDevice)
    {
        if (!IsIdleOrCold())
        {
            emitLogEntry("[AFD.TSBD.005] trySetBluetoothDevice() cannot proceed because the downloader is not cold", "FileDownloader", EAndroidLoggingLevel.Error);
            return false;
        }

        if (!tryInvalidateCachedInfrastructure()) //order
        {
            emitLogEntry("[AFD.TSBD.020] Failed to invalidate the cached-transport instance", "FileDownloader", EAndroidLoggingLevel.Error);
            return false;
        }

        _bluetoothDevice = bluetoothDevice; //order

        emitLogEntry("[AFD.TSBD.030] Successfully set the android-bluetooth-device to the given value", "FileDownloader", EAndroidLoggingLevel.Trace);

        return true;
    }

    public boolean tryInvalidateCachedInfrastructure()
    {
        boolean success1 = tryDisposeFilesystemManager(); // order
        boolean success2 = tryDisposeTransport(); //         order
        boolean success3 = tryDisposeCallbackProxy(); //     order

        return success1 && success2 && success3;
    }

    /**
     * Initiates a file download asynchronously. The progress is advertised through the callbacks provided by this class.
     * Setup interceptors for them to get informed about the status of the firmware-installation.
     *
     * @param remoteFilePath the remote-file-path to the file on the remote device that you wish to download
     * @param initialMtuSize sets the initial MTU for the connection that the McuMgr BLE-transport sets up for the firmware installation that will follow.
     *                       Note that if less than 0 it gets ignored and if it doesn't fall within the range [23, 517] it will cause a hard error.
     * @return a verdict indicating whether the file downloading was started successfully or not
     */
    public EAndroidFileDownloaderVerdict beginDownload(
            final String remoteFilePath,
            final int initialMtuSize
            // final int windowCapacity, //theoretically nordic firmwares at some point will support this for downloads   but as of Q3 2024 there is no support for this
    )
    {
        if (!IsCold()) //keep first
        {
            onError("[AFD.BD.000] Another download is already in progress");

            return EAndroidFileDownloaderVerdict.FAILED__DOWNLOAD_ALREADY_IN_PROGRESS;
        }

        if (remoteFilePath == null || remoteFilePath.isEmpty())
        {
            onError("[AFD.BD.010] Target-file provided is dud!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        _remoteFilePathSanitized = remoteFilePath.trim();
        if (_remoteFilePathSanitized.endsWith("/")) //the path must point to a file not a directory
        {
            onError("[AFD.BD.020] Provided target-path points to a directory not a file");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (!_remoteFilePathSanitized.startsWith("/"))
        {
            onError("[AFD.BD.030] Provided target-path is not an absolute path");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_context == null)
        {
            onError("[AFD.BD.040] No context specified - call trySetContext() first");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (_bluetoothDevice == null)
        {
            onError("[AFD.BD.050] No bluetooth-device specified - call trySetBluetoothDevice() first");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        try
        {
            resetDownloadState(); //order   must be called before ensureTransportIsInitializedExactlyOnce() because the environment might try to set the device via trySetBluetoothDevice()!!!
            ensureTransportIsInitializedExactlyOnce(initialMtuSize); //order
            setLoggingEnabledOnTransport(false); //order

            final EAndroidFileDownloaderVerdict verdict = ensureFilesystemManagerIsInitializedExactlyOnce(); //order
            if (verdict != EAndroidFileDownloaderVerdict.SUCCESS)
                return verdict;

            tryEnsureConnectionPriorityOnTransport(); //order
            ensureFileDownloaderCallbackProxyIsInitializedExactlyOnce(); //order

            setBusyState(true); //                         order
            setState(EAndroidFileDownloaderState.IDLE); // order

            _downloadingController = _fileSystemManager.fileDownload(_remoteFilePathSanitized, _fileDownloaderCallbackProxy); //order
        }
        catch (final Exception ex)
        {
            onError("[AFD.BD.060] Failed to initialize the download operation:\n\n" + ex, ex);

            return EAndroidFileDownloaderVerdict.FAILED__ERROR_UPON_COMMENCING;
        }

        return EAndroidFileDownloaderVerdict.SUCCESS;
    }

    public boolean tryPause()
    {
        if (_currentState == EAndroidFileDownloaderState.PAUSED)
            return true; // already paused which is ok

        if (_currentState != EAndroidFileDownloaderState.DOWNLOADING)
            return false;

        final TransferController transferController = _downloadingController;
        if (transferController == null)
            return false; //controller has been trashed

        try
        {
            transferController.pause();

            setState(EAndroidFileDownloaderState.PAUSED);
            setBusyState(false);

            setLoggingEnabledOnTransport(true);

            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFD.TP.010] [SUPPRESSED] Error while trying to pause the download:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Warning);
            return false;
        }
    }

    public boolean tryResume()
    {
        if (_currentState == EAndroidFileDownloaderState.DOWNLOADING)
            return true; //already downloading which is ok

        if (_currentState != EAndroidFileDownloaderState.PAUSED)
            return false;

        final TransferController transferController = _downloadingController;
        if (transferController == null)
            return false; //controller has been trashed

        try
        {
            transferController.resume();

            setState(EAndroidFileDownloaderState.DOWNLOADING);
            setBusyState(true);

            setLoggingEnabledOnTransport(false);

            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFD.TR.010] [SUPPRESSED] Error while trying to resume the download:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Warning);
            return false;
        }
    }

    public void nativeDispose()
    {
        emitLogEntry("[AFD.ND.010] Disposing the native-file-downloader", "file-downloader", EAndroidLoggingLevel.Trace);

        tryInvalidateCachedInfrastructure(); //  doesnt throw
        tryShutdownBackgroundExecutor(); //doesnt throw
    }

    @SuppressWarnings("UnusedReturnValue")
    public boolean tryDisconnect()
    {
        emitLogEntry("[AFD.TDISC.010] Will try to disconnect now ...", "file-downloader", EAndroidLoggingLevel.Trace);
        
        if (_transport == null)
        {
            emitLogEntry("[AFD.TDISC.020] Transport is null so nothing to disconnect from", "file-downloader", EAndroidLoggingLevel.Trace);
            return true;
        }

        try
        {
            _transport.release();
        }
        catch (Exception ex)
        {
            emitLogEntry("[AFD.TD.010] Failed to disconnect from the transport:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Error);
            return false;
        }

        return true;
    }

    @SuppressWarnings("UnusedReturnValue")
    private boolean tryShutdownBackgroundExecutor()
    {
        emitLogEntry("[AFD.TSBE.010] Shutting down the background-executor ...", "file-downloader", EAndroidLoggingLevel.Trace);

        try
        {
            _backgroundExecutor.shutdown();
            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFD.TBE.010] [SUPPRESSED] Error while shutting down background executor:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Warning);
            return false;
        }
    }

    private String _cancellationReason = "";

    public boolean tryCancel(final String reason)
    {
        _cancellationReason = reason;

        fireAndForgetInTheBg(() -> cancellingAdvertisement(reason)); //  order
        setState(EAndroidFileDownloaderState.CANCELLING); //             order

        final TransferController transferController = _downloadingController; //order
        if (transferController == null)
            return true; //nothing to cancel which is not an error

        try
        {
            transferController.cancel(); //order   keep this dead last
            return true;
        }
        catch (final Exception ex)
        {
            emitLogEntry("[AFD.TC.010] [SUPPRESSED] Error while trying to cancel the download:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Warning);
            return false;
        }
    }

    private void resetDownloadState()
    {
        _downloadStartTimestampInMs = 0;

        _lastBytesSent = 0;
        _lastBytesSentTimestampInMs = 0;

        setState(EAndroidFileDownloaderState.NONE);
        setBusyState(false);
    }

    private void ensureTransportIsInitializedExactlyOnce(int initialMtuSize)
    {
        if (_transport == null)
        {
            emitLogEntry("[AFD.ETIIEO.000] Transport is null - instantiating it now", "firmware-downloader", EAndroidLoggingLevel.Warning);

            _transport = new McuMgrBleTransport(_context, _bluetoothDevice);
        }

        if (initialMtuSize > 0)
        {
            _transport.setInitialMtu(initialMtuSize);
            emitLogEntry("[AFD.ETIIEO.010] Initial-MTU-size set explicitly to '" + initialMtuSize + "'", "firmware-downloader", EAndroidLoggingLevel.Info);
        }
        else
        {
            emitLogEntry("[AFD.ETIIEO.020] Initial-MTU-size left to its nordic-default-value which is probably 498", "firmware-downloader", EAndroidLoggingLevel.Info);
        }
    }

    private void ensureFileDownloaderCallbackProxyIsInitializedExactlyOnce()
    {
        if (_fileDownloaderCallbackProxy != null) //already initialized
            return;

        _fileDownloaderCallbackProxy = new FileDownloaderCallbackProxy();
    }

    private EAndroidFileDownloaderVerdict ensureFilesystemManagerIsInitializedExactlyOnce()
    {
        if (_fileSystemManager != null) //already initialized
            return EAndroidFileDownloaderVerdict.SUCCESS;

        emitLogEntry("[AFD.EFMIIEO.010] (Re)Initializing filesystem-manager", "file-downloader", EAndroidLoggingLevel.Trace);

        try
        {
            _fileSystemManager = new FsManager(_transport); //order
        }
        catch (final Exception ex)
        {
            onError("[AFD.EFMIIEO.020] Failed to initialize the native file-system-manager", ex); //sets the state to ERROR too!
            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        return EAndroidFileDownloaderVerdict.SUCCESS;
    }

    private void tryEnsureConnectionPriorityOnTransport()
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
            emitLogEntry("[AFD.TDT.010] Failed to release the transport:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Error);
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
            emitLogEntry("[AFD.TDFM.010] Failed to close the filesystem manager:\n\n" + ex, "file-downloader", EAndroidLoggingLevel.Error);
        }

        _fileSystemManager = null;
        return success;
    }

    private boolean tryDisposeCallbackProxy()
    {
        _fileDownloaderCallbackProxy = null;
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

    private void setState(final EAndroidFileDownloaderState newState)
    {
        setState(newState, 0, null);
    }

    private void setState(final EAndroidFileDownloaderState newState, final int totalBytesToBeDownloaded)
    {
        setState(newState, totalBytesToBeDownloaded, null);
    }

    private void setState(final EAndroidFileDownloaderState newState, final byte[] finalDataSnapshot)
    {
        setState(newState, 0, finalDataSnapshot);
    }

    private void setState(final EAndroidFileDownloaderState newState, final int totalBytesToBeDownloaded, final byte[] finalDataSnapshot)
    {
        if (_currentState == newState)
            return;

        final EAndroidFileDownloaderState oldState = _currentState; //               order
        _currentState = newState; //                                                 order
        final String remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized; //  order

        fireAndForgetInTheBg(() -> {
            stateChangedAdvertisement(oldState, newState); //order

            switch (newState)
            {
                case NONE: // * -> none
                    fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePathSanitizedSnapshot, 0, 0, 0);
                    break;
                case DOWNLOADING: // idle/paused -> uploading
                    if (oldState != EAndroidFileDownloaderState.IDLE && oldState != EAndroidFileDownloaderState.PAUSED)
                    {
                        logMessageAdvertisement("[AFD.SS.FAFITB.010] State changed to 'downloading' from an unexpected state '" + oldState + "' - this transition looks fishy so report this incident!", "file-downloader", EAndroidLoggingLevel.Warning.toString());
                    }

                    if (oldState != EAndroidFileDownloaderState.PAUSED) //todo   introduce a separate "FileDownloadResumingNow" event too
                    {
                        logMessageAdvertisement("[AFD.SS.FAFITB.025] Starting downloading of '" + totalBytesToBeDownloaded + "' bytes", "file-uploader", EAndroidLoggingLevel.Info.toString());

                        fileDownloadStartedAdvertisement(remoteFilePathSanitizedSnapshot, totalBytesToBeDownloaded); //order
                    }
                    break;
                case COMPLETE: // downloading -> complete
                    logMessageAdvertisement("[AFD.SS.FAFITB.030] Completed downloading of '" + totalBytesToBeDownloaded + "' bytes", "file-uploader", EAndroidLoggingLevel.Info.toString());

                    if (oldState != EAndroidFileDownloaderState.DOWNLOADING)
                    {
                        logMessageAdvertisement("[AFD.SS.FAFITB.035] State changed to 'complete' from an unexpected state '" + oldState + "' - this transition looks fishy so report this incident!", "file-downloader", EAndroidLoggingLevel.Warning.toString());
                    }

                    fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(remoteFilePathSanitizedSnapshot, 100, 0, 0); //00 order
                    fileDownloadCompletedAdvertisement(remoteFilePathSanitizedSnapshot, finalDataSnapshot); //                            order
                    break;
            }
        });

        //00  trivial hotfix to deal with the fact that the filedownload progress% doesnt fill up to 100%
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

    private void onError(final String errorMessage)
    {
        onError(errorMessage, /*exception*/ null);
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
        setState(EAndroidFileDownloaderState.ERROR);

        _lastFatalErrorMessage = errorMessage; //better set this directly in here considering that fatalErrorOccurredAdvertisement() is called only through onErrorImpl()

        final String remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized;

        fireAndForgetInTheBg(() -> fatalErrorOccurredAdvertisement(
                remoteFilePathSanitizedSnapshot,
                errorMessage,
                globalErrorCode
        ));
    }

    public void fatalErrorOccurredAdvertisement(
            final String remoteFilePath,
            final String errorMessage,
            final int globalErrorCode // have a look at EGlobalErrorCode.cs in csharp
    )
    {
    }

    private void emitLogEntry(final String message, final String category, final EAndroidLoggingLevel level)
    {
        fireAndForgetInTheBg(() -> logMessageAdvertisement(message, category, level.toString())); //todo  include the remoteFilePath as well
    }

    @Contract(pure = true)
    public void cancellingAdvertisement(final String reason)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void busyStateChangedAdvertisement(final boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void cancelledAdvertisement(final String reason)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true) //wrapper utility method so that we will not have to constantly pass remoteFilePathSanitized as the first argument    currently unused but should be handy in the future
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
    public void fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(final String resourceId, final int progressPercentage, final float currentThroughputInKBps, final float totalAverageThroughputInKBps)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileDownloadStartedAdvertisement(final String resourceId, long totalBytesToBeDownloaded)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true)
    public void fileDownloadCompletedAdvertisement(final String resourceId, final byte[] data)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    @Contract(pure = true) //wrapper utility method so that we will not have to constantly pass remoteFilePathSanitized as the fourth argument    currently unused but should be handy in the future
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
        public void onDownloadProgressChanged(final int totalBytesSentSoFar, final int fileSize, final long timestampInMs)
        {
            setState(EAndroidFileDownloaderState.DOWNLOADING, fileSize);
            setBusyState(true);

            final String remoteFilePathSanitizedSnapshot = _remoteFilePathSanitized; //order
            fireAndForgetInTheBg(() -> {
                int fileDownloadProgressPercentage = (int) (totalBytesSentSoFar * 100.f / fileSize);
                float currentThroughputInKBps = calculateCurrentThroughputInKBps(totalBytesSentSoFar, timestampInMs);
                float totalAverageThroughputInKBps = calculateTotalAverageThroughputInKBps(totalBytesSentSoFar, timestampInMs);

                fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement( // convert to percent
                        remoteFilePathSanitizedSnapshot,
                        fileDownloadProgressPercentage,
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
            if (_downloadStartTimestampInMs == 0) {
                _downloadStartTimestampInMs = timestampInMs;
                return 0;
            }

            float elapsedSecondSinceDownloadStart = ((float)(timestampInMs - _downloadStartTimestampInMs)) / 1_000;
            if (elapsedSecondSinceDownloadStart == 0) { //should be impossible but just in case
                return 0;
            }

            return (float)(totalBytesSentSoFar) / (elapsedSecondSinceDownloadStart * 1_024);
        }

        @Override
        public void onDownloadFailed(@NonNull final McuMgrException exception)
        {
            onError("[AFD.ODF.010] Download failed due to an error:\n\n" + exception, exception);
            setBusyState(false);

            setLoggingEnabledOnTransport(true);

            _downloadingController = null; //game over
        }

        @Override
        public void onDownloadCanceled()
        {
            setState(EAndroidFileDownloaderState.CANCELLED); //                         order
            fireAndForgetInTheBg(() -> cancelledAdvertisement(_cancellationReason)); // order
            setBusyState(false); //                                                     order

            setLoggingEnabledOnTransport(true);
            _downloadingController = null; //game over
        }

        @Override
        public void onDownloadCompleted(byte @NotNull [] data)
        {
            //fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(_remoteFilePathSanitized, 100, 0, 0); //no need this is taken care of inside setState()

            setState(EAndroidFileDownloaderState.COMPLETE, data);
            setBusyState(false);

            setLoggingEnabledOnTransport(true);
            _downloadingController = null; //game over
        }
    }
}
