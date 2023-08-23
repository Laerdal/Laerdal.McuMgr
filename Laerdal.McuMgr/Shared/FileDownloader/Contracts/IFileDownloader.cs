// ReSharper disable UnusedMember.Global
// ReSharper disable EventNeverSubscribedTo.Global

namespace Laerdal.McuMgr.FileDownloader.Contracts
{
    /// <summary>Downloads a file on a specific Nordic-chip-based BLE device</summary>
    /// <remarks>For the file-downloading process to even commence you need to be authenticated with the AED device that's being targeted.</remarks>
    public interface IFileDownloader : IFileDownloaderEventSubscribable, IFileDownloaderCommands
    {
    }
}
