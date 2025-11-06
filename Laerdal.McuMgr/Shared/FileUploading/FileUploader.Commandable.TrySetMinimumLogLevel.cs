// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Threading.Tasks;
using Laerdal.McuMgr.Common.Enums;
using Laerdal.McuMgr.Common.Events;
using Laerdal.McuMgr.FileUploading.Contracts.Enums;
using Laerdal.McuMgr.FileUploading.Contracts.Exceptions;

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
