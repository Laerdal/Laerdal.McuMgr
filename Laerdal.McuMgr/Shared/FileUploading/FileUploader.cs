// ReSharper disable UnusedType.Global
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.Common.AsyncX;
using Laerdal.McuMgr.FileUploading.Contracts;
using Laerdal.McuMgr.FileUploading.Contracts.Native;

namespace Laerdal.McuMgr.FileUploading
{
    /// <inheritdoc cref="IFileUploader"/>
    public partial class FileUploader : IFileUploader, IFileUploaderEventEmittable
    {
        protected bool IsOperationOngoing;
        protected bool IsCancellationRequested;
        protected string CancellationReason = "";
        protected readonly object OperationCheckLock = new();
        protected readonly INativeFileUploaderProxy NativeFileUploaderProxy;

        public string LastFatalErrorMessage => NativeFileUploaderProxy?.LastFatalErrorMessage;
        
        protected readonly AsyncManualResetEvent KeepGoing = new(set: true); //related to pausing/unpausing   keepgoing=true by default

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFileUploaderProxy
        internal FileUploader(INativeFileUploaderProxy nativeFileUploaderProxy)
        {
            NativeFileUploaderProxy = nativeFileUploaderProxy ?? throw new ArgumentNullException(nameof(nativeFileUploaderProxy));
            NativeFileUploaderProxy.FileUploader = this; //vital
        }
    }
}
