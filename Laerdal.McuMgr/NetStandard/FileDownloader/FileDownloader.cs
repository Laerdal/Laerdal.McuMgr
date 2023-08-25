// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.FileDownloader.Contracts;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
        public FileDownloader(object bluetoothDevice) => throw new NotImplementedException();
    }
}
