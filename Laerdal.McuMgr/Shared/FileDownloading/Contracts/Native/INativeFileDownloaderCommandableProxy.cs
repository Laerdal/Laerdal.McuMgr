using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    public interface INativeFileDownloaderCommandableProxy
    {
        /// <summary>Sets the minimum native-log-level for the ongoing download operation</summary>
        /// <param name="minimumNativeLogLevel">The minimum log level to set</param>
        /// <returns>Always returns true</returns>
        bool TrySetMinimumNativeLogLevel(ELogLevel minimumNativeLogLevel);
        
        /// <summary>Pauses the file-uploading process</summary>
        /// <returns>True if the pausing request was successfully effectuated (or if the transfer was already paused) - False otherwise which typically means that the underlying transport has been dispoed</returns>
        bool TryPause();
        
        /// <summary>Resumes the file-uploading process</summary>
        /// <returns>True if the resumption request was successfully effectuated (or if the transfer has already been resumed) - False otherwise which typically means is nothing to resume</returns>        
        bool TryResume();
        
        /// <summary>Cancels the file-uploading process</summary>
        /// <param name="reason">(optional) The reason for the cancellation</param>
        /// <returns>True if the cancellation request was successfully sent to the underlying native implementation (or if there is no transfer ongoing to cancel) - False otherwise which typically means there was an internal error (very rare)</returns>
        bool TryCancel(string reason = "");
        
        /// <summary>Disconnects the file-downloader from the targeted device</summary>
        bool TryDisconnect();
        
        /// <summary>Begins the file-downloading process</summary>
        /// <param name="remoteFilePath">The remote file path to download from</param>
        /// <param name="minimumNativeLogLevel">(optional) The minimum native log level to use for the download operation</param>
        /// <param name="initialMtuSize">(optional) The initial MTU size to use for the download operation</param>
        /// <returns>The verdict of the begin-download request</returns>
        EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, ELogLevel? minimumNativeLogLevel = null, int? initialMtuSize = null);

        /// <summary>Sets the context object to be used by the native implementation - only Android actually honors this</summary>
        /// <param name="context">The context object to set</param>
        /// <returns>True if the context was successfully set - False otherwise</returns>
        bool TrySetContext(object context);
        
        /// <summary>Sets the bluetooth device object to be used as the bluetooth target - can only be performed if there is no ongoing download operation</summary>
        /// <param name="bluetoothDevice">The bluetooth device object to set</param>
        /// <returns>True if the bluetooth device was successfully set - False otherwise</returns>
        bool TrySetBluetoothDevice(object bluetoothDevice);
        
        /// <summary>Invalidates any cached native-infrastructure inside the native implementation (transport layers) - can only be performed if there is no ongoing download operation</summary>
        /// <returns>True if the cached infrastructure was successfully invalidated - False otherwise</returns>
        bool TryInvalidateCachedInfrastructure();
    }
}
