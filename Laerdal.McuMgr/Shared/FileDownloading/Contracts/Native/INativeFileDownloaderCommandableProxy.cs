using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    internal interface INativeFileDownloaderCommandableProxy
    {
        bool TryPause();
        bool TryResume();
        bool TryCancel(string reason = "");

        bool TryDisconnect();
        EFileDownloaderVerdict BeginDownload(string remoteFilePath, int? initialMtuSize = null);

        bool TrySetContext(object context);
        bool TrySetBluetoothDevice(object bluetoothDevice);
        bool TryInvalidateCachedInfrastructure();
    }
}
