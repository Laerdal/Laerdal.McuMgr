using System;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller, IFirmwareInstallerEventEmittable
    {
        protected readonly INativeFirmwareInstallerProxy NativeFirmwareInstallerProxy;

        protected bool IsOperationOngoing;
        protected readonly object OperationCheckLock = new();
        
        public string LastFatalErrorMessage => NativeFirmwareInstallerProxy?.LastFatalErrorMessage;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFirmwareInstallerProxy
        internal FirmwareInstaller(INativeFirmwareInstallerProxy nativeFirmwareInstallerProxy)
        {
            NativeFirmwareInstallerProxy = nativeFirmwareInstallerProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareInstallerProxy));
            NativeFirmwareInstallerProxy.FirmwareInstaller = this; //vital
        }
    }
}
