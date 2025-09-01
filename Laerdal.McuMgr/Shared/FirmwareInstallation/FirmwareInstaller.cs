using System;
using Laerdal.McuMgr.FirmwareInstallation.Contracts;
using Laerdal.McuMgr.FirmwareInstallation.Contracts.Native;

namespace Laerdal.McuMgr.FirmwareInstallation
{
    /// <inheritdoc cref="IFirmwareInstaller"/>
    public partial class FirmwareInstaller : IFirmwareInstaller, IFirmwareInstallerEventEmittable
    {
        private readonly INativeFirmwareInstallerProxy _nativeFirmwareInstallerProxy;

        protected bool IsOperationOngoing;
        protected readonly object OperationCheckLock = new();
        
        public string LastFatalErrorMessage => _nativeFirmwareInstallerProxy?.LastFatalErrorMessage;

        //this constructor is also needed by the testsuite    tests absolutely need to control the INativeFirmwareInstallerProxy
        internal FirmwareInstaller(INativeFirmwareInstallerProxy nativeFirmwareInstallerProxy)
        {
            _nativeFirmwareInstallerProxy = nativeFirmwareInstallerProxy ?? throw new ArgumentNullException(nameof(nativeFirmwareInstallerProxy));
            _nativeFirmwareInstallerProxy.FirmwareInstaller = this; //vital
        }
    }
}
