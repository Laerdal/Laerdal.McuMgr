// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantExtendsListEntry

using System;
using Laerdal.McuMgr.FirmwareInstaller.Contracts;
using Laerdal.McuMgr.FirmwareInstaller.Contracts.Enums;

namespace Laerdal.McuMgr.FirmwareInstaller
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller
    {
        public FirmwareInstaller(object bluetoothDevice) => throw new NotImplementedException();

        public string LastFatalErrorMessage => throw new NotImplementedException();

        public EFirmwareInstallationVerdict BeginInstallation(
            byte[] data,
            EFirmwareInstallationMode mode = EFirmwareInstallationMode.TestAndConfirm,
            bool? eraseSettings = null,
            int? estimatedSwapTimeInMilliseconds = null,
            int? windowCapacity = null,
            int? memoryAlignment = null,
            int? pipelineDepth = null,
            int? byteAlignment = null
        )
        {
            throw new NotImplementedException();
        }

        public void Cancel() => throw new NotImplementedException();
        public void Disconnect() => throw new NotImplementedException();
    }
}
