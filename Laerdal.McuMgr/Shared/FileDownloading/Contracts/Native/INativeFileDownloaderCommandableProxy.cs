using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileDownloading.Contracts.Enums;

namespace Laerdal.McuMgr.FileDownloading.Contracts.Native
{
    public interface INativeFileDownloaderCommandableProxy
    {
        bool TryPause();
        bool TryResume();
        bool TryCancel(string reason = "");
        bool TrySetMinimumLogLevel(ELogLevel minimumLogLevel);

        bool TryDisconnect();
        EFileDownloaderVerdict NativeBeginDownload(string remoteFilePath, ELogLevel? minimumLogLevel = null, int? initialMtuSize = null);

        bool TrySetContext(object context);
        bool TrySetBluetoothDevice(object bluetoothDevice);
        bool TryInvalidateCachedInfrastructure();
    }
}
