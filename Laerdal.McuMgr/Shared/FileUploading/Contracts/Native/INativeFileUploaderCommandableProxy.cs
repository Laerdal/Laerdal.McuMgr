using Laerdal.McuMgr.FileUploading.Contracts.Enums;

namespace Laerdal.McuMgr.FileUploading.Contracts.Native
{
    internal interface INativeFileUploaderCommandableProxy
    {
        void Cancel(string reason = "");
        void Disconnect();

        EFileUploaderVerdict BeginUpload(
            string remoteFilePath,
            byte[] data,
            int? initialMtuSize = null,
            int? pipelineDepth = null,
            int? byteAlignment = null,
            int? windowCapacity = null,
            int? memoryAlignment = null
        );

        bool TrySetContext(object context);
        bool TrySetBluetoothDevice(object bluetoothDevice);
        bool TryInvalidateCachedTransport();
    }
}
