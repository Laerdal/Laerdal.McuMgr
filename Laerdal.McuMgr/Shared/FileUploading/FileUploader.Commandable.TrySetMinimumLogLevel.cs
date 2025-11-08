// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using Laerdal.McuMgr.Common.Enums;

namespace Laerdal.McuMgr.FileUploading
{
    public partial class FileUploader
    {
        public bool TrySetMinimumLogLevel(ELogLevel minimumLogLevel)
        {
            return NativeFileUploaderProxy?.TrySetMinimumLogLevel(minimumLogLevel) ?? true;
        }
    }
}
