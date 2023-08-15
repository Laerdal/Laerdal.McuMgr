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
    private TransferController _controller;

    private int _initialBytes;
    private long _downloadStartTimestamp;
    private String _remoteFilePathSanitized = "";
    private EAndroidFileDownloaderState _currentState = EAndroidFileDownloaderState.NONE;

    public AndroidFileDownloader(@NonNull final Context context, @NonNull final BluetoothDevice bluetoothDevice)
    {
        _transport = new McuMgrBleTransport(context, bluetoothDevice);
    }

    public EAndroidFileDownloaderVerdict beginDownload(final String remoteFilePath)
    {
        if (_currentState != EAndroidFileDownloaderState.NONE  //if the download is already in progress we bail out
                && _currentState != EAndroidFileDownloaderState.ERROR
                && _currentState != EAndroidFileDownloaderState.COMPLETE
                && _currentState != EAndroidFileDownloaderState.CANCELLED)
        {
            return EAndroidFileDownloaderVerdict.FAILED__DOWNLOAD_ALREADY_IN_PROGRESS;
        }

        if (remoteFilePath == null || remoteFilePath.isEmpty()) {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement("Provided target-path is empty!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        final String remoteFilePathSanitized = remoteFilePath.trim();
        if (remoteFilePathSanitized.endsWith("/")) //the path must point to a file not a directory
        {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement("Provided target-path points to a directory not a file!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        if (!remoteFilePathSanitized.startsWith("/"))
        {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement("Provided target-path is not an absolute path!");

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        try
        {
            _fileSystemManager = new FsManager(_transport);
        }
        catch (final Exception ex)
        {
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement(ex.getMessage());

            return EAndroidFileDownloaderVerdict.FAILED__INVALID_SETTINGS;
        }

        setLoggingEnabled(false);
        requestHighConnectionPriority();

        setState(EAndroidFileDownloaderState.IDLE);
        busyStateChangedAdvertisement(true);
        fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(0, 0);

        _initialBytes = 0;

        _remoteFilePathSanitized = remoteFilePathSanitized;
        _controller = _fileSystemManager.fileDownload(remoteFilePathSanitized, new FileDownloaderCallbackProxy());

        return EAndroidFileDownloaderVerdict.SUCCESS;
    }

    public void pause()
    {
        final TransferController transferController = _controller;
        if (transferController == null)
            return;

        setState(EAndroidFileDownloaderState.PAUSED);
        setLoggingEnabled(true);
        transferController.pause();
        busyStateChangedAdvertisement(false);
    }

    public void resume()
    {
        final TransferController transferController = _controller;
        if (transferController == null)
            return;

        setState(EAndroidFileDownloaderState.DOWNLOADING);

        busyStateChangedAdvertisement(true);
        _initialBytes = 0;

        setLoggingEnabled(false);
        transferController.resume();
    }

    public void disconnect()
    {
        _fileSystemManager.getTransporter().release();
    }

    public void cancel()
    {
        setState(EAndroidFileDownloaderState.CANCELLING); //order

        final TransferController transferController = _controller;
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
            fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(100, 0);
        }

        //00 trivial hotfix to deal with the fact that the filedownload progress% doesnt fill up to 100%
    }

    private String _lastFatalErrorMessage;

    public String getLastFatalErrorMessage()
    {
        return _lastFatalErrorMessage;
    }

    public void fatalErrorOccurredAdvertisement(final String errorMessage)
    {
        _lastFatalErrorMessage = errorMessage; //this method is meant to be overridden by csharp binding libraries to intercept updates
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

    public void fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(final int progressPercentage, final float averageThroughput)
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

            fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement( // convert to percent
                    fileDownloadProgressPercentage,
                    transferSpeed
            );
        }

        @Override
        public void onDownloadFailed(@NonNull final McuMgrException error)
        {
            fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(0, 0);
            setState(EAndroidFileDownloaderState.ERROR);
            fatalErrorOccurredAdvertisement(error.getMessage());
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onDownloadCanceled()
        {
            fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(0, 0);
            setState(EAndroidFileDownloaderState.CANCELLED);
            cancelledAdvertisement();
            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }

        @Override
        public void onDownloadCompleted(byte @NotNull [] data)
        {
            //fileDownloadProgressPercentageAndThroughputDataChangedAdvertisement(100, 0); //no need this is taken care of inside setState()

            downloadCompletedAdvertisement(_remoteFilePathSanitized, data); //    order  vital
            setState(EAndroidFileDownloaderState.COMPLETE); //                    order  vital

            setLoggingEnabled(true);
            busyStateChangedAdvertisement(false);
        }
    }
}
