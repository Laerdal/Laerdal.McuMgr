// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TrySetMinimumNativeLogLevel(ELogLevel minimumNativeLogLevel)
        {
            return NativeFileUploaderProxy?.TrySetMinimumNativeLogLevel(minimumNativeLogLevel) ?? true;
        }
    }
}
