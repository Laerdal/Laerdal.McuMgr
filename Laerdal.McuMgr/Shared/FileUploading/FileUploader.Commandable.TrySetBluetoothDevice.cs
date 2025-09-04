namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TrySetBluetoothDevice(object bluetoothDevice) => NativeFileUploaderProxy?.TrySetBluetoothDevice(bluetoothDevice) ?? false;
    }
}
