using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    internal interface INativeFileDownloaderCommandableProxy
    {
        void Cancel();
        void Disconnect();
        EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null);
        
        bool TrySetContext(object context);
        bool TrySetBluetoothDevice(object bluetoothDevice);
        bool TryInvalidateCachedTransport();
    }
}