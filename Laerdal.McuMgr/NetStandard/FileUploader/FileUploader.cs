// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using System.Linq;

namespace Laerdal.McuMgr.FileUploader
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader
    {
        public FileUploader(object bleDevice) => throw new NotImplementedException();

        public string LastFatalErrorMessage => throw new NotImplementedException();

        public IFileUploader.EFileUploaderVerdict BeginUpload(string path, byte[] data) => throw new NotImplementedException();

        public void Cancel() => throw new NotImplementedException();
        public void Disconnect() => throw new NotImplementedException();
    }
}
