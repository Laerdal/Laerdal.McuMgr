// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

using System;

namespace Laerdal.McuMgr.FileUploading.Contracts
{
    /// <summary>Uploads a file on a specific Nordic-chip-based BLE device</summary>
    /// <remarks>For the file-uploading process to even commence you need to be authenticated with the AED device that's being targeted.</remarks>
    public interface IFileUploader :
        IDisposable,
        IFileUploaderQueryable,
        IFileUploaderCommandable,
        IFileUploaderCleanupable,
        IFileUploaderEventSubscribable
        //IFileUploaderEventEmittable   dont   this interface is meant to be internal only
    {
    }
}
