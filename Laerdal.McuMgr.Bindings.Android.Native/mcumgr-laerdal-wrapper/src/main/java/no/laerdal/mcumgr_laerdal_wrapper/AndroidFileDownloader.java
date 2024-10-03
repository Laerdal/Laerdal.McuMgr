package no.laerdal.mcumgr_laerdal_wrapper;

import android.bluetooth.BluetoothDevice;
import android.content.Context;
import androidx.annotation.NonNull;
import io.runtime.mcumgr.McuMgrTransport;
import io.runtime.mcumgr.ble.McuMgrBleTransport;
import io.runtime.mcumgr.exception.McuMgrException;
import io.runtime.mcumgr.managers.FsManager;
import io.runtime.mcumgr.transfer.DownloadCallback;
import io.runtime.mcumgr.transfer.TransferController;
import no.nordicsemi.android.ble.ConnectionPriorityRequest;
import org.jetbrains.annotations.NotNull;

@SuppressWarnings("unused")
public class AndroidFileDownloader
{
    private FsManager _fileSystemManager;
    @SuppressWarnings("FieldCanBeLocal")
    private final McuMgrBleTransport _transport;
    private TransferController _downloadingController;

    private int _initialBytes;
    private long _downloadStartTimestamp;
    private String _remoteFilePathSanitized = "";
    private EAndroidFileDownloaderState _currentState = EAndroidFileDownloaderState.NONE;

    public AndroidFileDownloader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _transport = new McuMgrBleTransport(context, bluetoothDevice);
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
            // final int memoryAlignment //this doesnt make sense for downloading   it only makes sense in uploading scenarios    https://github.com/NordicSemiconductor/Android-nRF-Connect-Device-Manager/issues/188#issuecomment-2391146897
    )
    {
        if (_currentState != EAndroidFileDownloaderState.NONE  //if the download is already in progress we bail out
                && _currentState != EAndroidFileDownloaderState.ERROR
                && _currentState != EAndroidFileDownloaderState.COMPLETE
                && _currentState != EAndroidFileDownloaderState.CANCELLED)
        {
            logMessageAdvertisement("Cannot start a new download while another one is still in progress (state=" + _currentState.toString() + ")", "FileDownloader", "ERROR", remoteFilePath);

            return EAndroidFileDownloaderVerdict.FAILED__DOWNLOAD_ALREADY_IN_PROGRESS;
        }

        if (remoteFilePath == null || remoteFilePath.isEmpty()) {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement("", "Target-file provided is dud!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        final String remoteFilePathSanitized = remoteFilePath.trim();
        if (remoteFilePathSanitized.endsWith("/")) //the path must point to a file not a directory
        {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, "Provided target-path points to a directory not a file!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (!remoteFilePathSanitized.startsWith("/"))
        {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, "Provided target-path is not an absolute path!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (initialMtuSize > 0)
        {
            _transport.setInitialMtu(initialMtuSize);
        }

        _fileSystemManager = new FsManager(_transport);

        setLoggingEnabled(false);
        requestHighConnectionPriority();

        setState(EAndroidFileDownloaderState.IDLE);
        busyStateChangedAdvertisement(true);
        fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);

        _initialBytes = 0;
        _remoteFilePathSanitized = remoteFilePathSanitized;

        try
        {
            _downloadingController = _fileSystemManager.fileDownload(remoteFilePathSanitized, new FileDownloaderCallbackProxy());
        }
        catch (final Exception ex)
        {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, ex.getMessage());

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

    private void requestHighConnectionPriority()
    {
        final McuMgrTransport mcuMgrTransporter = _fileSystemManager.getTransporter();
        if (!(mcuMgrTransporter instanceof McuMgrBleTransport))
            return;

        final McuMgrBleTransport bleTransporter = (McuMgrBleTransport) mcuMgrTransporter;
        bleTransporter.requestConnPriority(ConnectionPriorityRequest.CONNECTION_PRIORITY_HIGH);
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

    private String _lastFatalErrorMessage;

    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    public void fatalErrorOccurredAdvertisement(final String resource, final String errorMessage)
    {
        //this method is meant to be overridden by csharp binding libraries to intercept updates
        _lastFatalErrorMessage = errorMessage;
    }

    public void busyStateChangedAdvertisement(boolean busyNotIdle)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void cancelledAdvertisement()
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    //wrapper utility method so that we wont have to constantly pass remoteFilePathSanitized as the first argument    currently unused but should be handy in the future
    public void stateChangedAdvertisement(final EAndroidFileDownloaderState oldState, final EAndroidFileDownloaderState newState)
    {
        stateChangedAdvertisement(_remoteFilePathSanitized, oldState, newState);
    }

    public void stateChangedAdvertisement(final String resource, final EAndroidFileDownloaderState oldState, final EAndroidFileDownloaderState newState)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(final int progressPercentage, final float averageThroughput)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    public void downloadCompletedAdvertisement(final String resource, final byte[] data)
    {
        //this method is intentionally empty   its meant to be overridden by csharp binding libraries to intercept updates
    }

    //wrapper utility method so that we wont have to constantly pass remoteFilePathSanitized as the fourth argument    currently unused but should be handy in the future
    private void logMessageAdvertisement(final String message, final String category, final String level)
    {
        logMessageAdvertisement(message, category, level, _remoteFilePathSanitized);
    }

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
        public void onDownloadFailed(@NonNull final McuMgrException error)
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement(_remoteFilePathSanitized, error.getMessage());
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onDownloadCanceled()
        {
            fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(0, 0);
            setState(EAndroidFileDownloaderState.CANCELLED);
            cancelledAdvertisement();
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onDownloadCompleted(byte @NotNull [] data)
        {
            //fileDownloadProgressPercentageAndDataThroughputChangedAdvertisement(100, 0); //no need this is taken care of inside setState()

            setState(EAndroidFileDownloaderState.COMPLETE); //                    order  vital
            downloadCompletedAdvertisement(_remoteFilePathSanitized, data); //    order  vital

            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }
    }
}
