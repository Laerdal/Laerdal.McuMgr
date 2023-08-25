// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.FileUploader.Contracts;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        public FileUploader(object bluetoothDevice) => throw new NotImplementedException();
    }
}
