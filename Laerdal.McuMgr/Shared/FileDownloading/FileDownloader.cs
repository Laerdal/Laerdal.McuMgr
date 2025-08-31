// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.FileDownloading.Contracts;
using Laerdal.McuMgr.FileDownloading.Contracts.Native;

namespace Laerdal.McuMgr.FileDownloading
{
    /// <inheritdoc cref="IFileDownloader"/>
    public partial class FileDownloader : IFileDownloader, IFileDownloaderEventEmittable
    {
        private readonly INativeFileDownloaderProxy _nativeFileDownloaderProxy;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileDownloaderProxy
        internal FileDownloader(INativeFileDownloaderProxy nativeFileDownloaderProxy)
        {
            _nativeFileDownloaderProxy = nativeFileDownloaderProxy ?? throw new ArgumentNullException(nameof(nativeFileDownloaderProxy));
            _nativeFileDownloaderProxy.FileDownloader = this; //vital
        }

        public string LastFatalErrorMessage => _nativeFileDownloaderProxy?.LastFatalErrorMessage;
    }
}