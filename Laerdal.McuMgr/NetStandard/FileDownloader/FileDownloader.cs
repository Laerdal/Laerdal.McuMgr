// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Linq;

namespace Laerdal.McuMgr.FileDownloader
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader
    {
        public FileDownloader(object bluetoothDevice) => throw new NotImplementedException();

        public string LastFatalErrorMessage => throw new NotImplementedException();

        public IFileDownloader.EFileDownloaderVerdict BeginDownload(string path) => throw new NotImplementedException();

        public void Cancel() => throw new NotImplementedException();
        public void Disconnect() => throw new NotImplementedException();
    }
}