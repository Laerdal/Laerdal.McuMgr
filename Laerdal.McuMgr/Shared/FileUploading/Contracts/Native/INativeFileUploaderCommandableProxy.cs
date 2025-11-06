using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    public interface INativeFileUploaderCommandableProxy
    {
        bool TryPause();
        bool TryResume();
        bool TryCancel(string reason = "");
        bool TryDisconnect();

        EFileUploaderVerdict NativeBeginUpload(
            byte[] data,
            string resourceId,
            string remoteFilePath,
            ELogLevel? minimumLogLevel = null,
            int? initialMtuSize = null,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        );

        bool TrySetContext(object context);
        bool TrySetBluetoothDevice(object bluetoothDevice);
        bool TryInvalidateCachedInfrastructure();
    }
}
